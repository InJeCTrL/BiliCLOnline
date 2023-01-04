﻿using BiliCLOnline.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static BiliCLOnline.Utils.Constants;

namespace BiliCLOnline.Utils
{
    public class WebHelper
    {
        /// <summary>
        /// 用于请求BilibiliAPI的httpclient
        /// </summary>
        private readonly HttpClient BiliRequestClient = new();

        /// <summary>
        /// 用于请求BilibiliAPI的httpclient(无代理)
        /// </summary>
        private readonly HttpClient BiliRequestLocalClient = new();

        /// <summary>
        /// 用于请求验证码服务的httpclient
        /// </summary>
        private readonly HttpClient HCaptchaClient = new();

        /// <summary>
        /// 用于跳转分享链接的httpclient
        /// </summary>
        private readonly HttpClient BiliJumpRequestClient = new(new HttpClientHandler 
        {
            AllowAutoRedirect = false
        })
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        /// <summary>
        /// 并发请求网络的信号量
        /// </summary>
        private readonly SemaphoreSlim ConcurrentLimit = new(MaxConcurrentFetchLimit);

        /// <summary>
        /// ScrapingAnt token列表
        /// </summary>
        private readonly string[] SAkeys;

        /// <summary>
        /// 当前使用的ScrapingAnt token序号
        /// </summary>
        private int idxSAkey = 0;

        /// <summary>
        /// SA序号锁
        /// </summary>
        private readonly object mSAidx = new object();

        private readonly ILogger<WebHelper> logger;

        public WebHelper(ILogger<WebHelper> _logger)
        {
            SAkeys = (Environment.GetEnvironmentVariable("SAKeys") ?? "").Split(";", StringSplitOptions.RemoveEmptyEntries);
            BiliRequestClient.DefaultRequestHeaders.Add("x-api-key", SAkeys[0]);
            logger = _logger;
        }

        /// <summary>
        /// 校验验证码token是否正确
        /// </summary>
        /// <param name="token">验证码token</param>
        /// <param name="secret">hcaptcha密钥</param>
        /// <returns>是否通过验证</returns>
        public async Task<bool> VerifyCaptcha(string token, string secret)
        {
            try
            {
                var postData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("response", token),
                    new KeyValuePair<string, string>("secret", secret)
                };

                using var response = await HCaptchaClient.PostAsync(
                    HCaptchaVerifyURL,
                    new FormUrlEncodedContent(postData)
                    );
                response.EnsureSuccessStatusCode();

                var hCaptchaReturn = await response.Content.ReadFromJsonAsync<HCaptchaReturn>();

                return hCaptchaReturn.success;
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is JsonException)
            {
                logger.LogError(message: ex.ToString());
            }

            return false;
        }

        /// <summary>
        /// 获取B站API的响应JSON
        /// </summary>
        /// <param name="URL">B站APIURL</param>
        /// <typeparam name="T">IReturnData</typeparam>
        /// <returns>响应JSON</returns>
        public async Task<BilibiliAPIReturn<T>> GetResponse<T>(string URL) where T : ReturnData
        {
            BilibiliAPIReturn<T> responseJSON = default;
            WebAPIReturn responseWrapper = default;

            await ConcurrentLimit.WaitAsync();

            #region 使用无代理httpclient尝试
            var localSucc = false;

            try
            {
                using var responseMsg = await BiliRequestLocalClient.GetAsync(URL);
                responseMsg.EnsureSuccessStatusCode();
                responseJSON = await responseMsg.Content.ReadFromJsonAsync<BilibiliAPIReturn<T>>();

                if (responseJSON.code == 412)
                {
                    throw new HttpRequestException(URL);
                }

                localSucc = true;
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is JsonException || ex is TaskCanceledException)
            {
                logger.LogError(message: $"Exception: [{ex}] url: [{URL}]");
            }

            if (localSucc)
            {
                ConcurrentLimit.Release();
                return responseJSON;
            }
            #endregion

            try
            {
                #region 使用有代理的httpclient
                URL = $"{ScrapingAntAPIPrefix}{HttpUtility.UrlEncode(URL)}";

                do
                {
                    int currentSAidx = idxSAkey;
                    try
                    {
                        using var responseMsg = await BiliRequestClient.GetAsync(URL);
                        responseMsg.EnsureSuccessStatusCode();
                        responseWrapper = await responseMsg.Content.ReadFromJsonAsync<WebAPIReturn>();

                        responseJSON = JsonSerializer.Deserialize<BilibiliAPIReturn<T>>(responseWrapper.content);
                    }
                    catch (JsonException)
                    {
                        continue;
                    }
                    catch (HttpRequestException ex)
                    {
                        if (ex.StatusCode.Value == HttpStatusCode.Forbidden)
                        {
                            logger.LogWarning(message: $"Warning: ScrapingAnt limit exceed [{ex}] url: [{URL}]");
                            // 其它并发线程已推进了idxSAkey
                            if (currentSAidx < idxSAkey)
                            {
                                continue;
                            }
                            lock (mSAidx)
                            {
                                // 其它并发线程已推进了idxSAkey
                                if (currentSAidx < idxSAkey)
                                {
                                    continue;
                                }
                                // 此线程应当更换SAkey
                                else
                                {
                                    ++idxSAkey;
                                    // 超出token列表
                                    if (idxSAkey >= SAkeys.Length)
                                    {
                                        throw;
                                    }

                                    BiliRequestClient.DefaultRequestHeaders.Remove("x-api-key");
                                    BiliRequestClient.DefaultRequestHeaders.Add("x-api-key", SAkeys[idxSAkey]);
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            throw;
                        }
                    }

                    if (responseJSON.code != 412)
                    {
                        break;
                    }
                } while (true);
                #endregion
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(message: $"Exception: [{ex}] url: [{URL}]");
                ConcurrentLimit.Release();
                throw;
            }
            catch (JsonException ex)
            {
                logger.LogError(message: $"Exception: [{ex}] url: [{URL}] content: [{responseWrapper.content}]");
                ConcurrentLimit.Release();
                throw;
            }
            
            ConcurrentLimit.Release();

            return responseJSON;
        }

        /// <summary>
        /// 获取B站分享短链接重定向跳转的目标URL
        /// </summary>
        /// <param name="URL">B站分享短链接</param>
        /// <returns>目标URL</returns>
        public async Task<string> GetRedirect(string URL)
        {
            string redirectURL = string.Empty;

            try
            {
                using var Response = await BiliJumpRequestClient.GetAsync(URL);

                if (Response.StatusCode == HttpStatusCode.Redirect)
                {
                    redirectURL = Response.Headers.Location.ToString();
                }
                else
                {
                    throw new HttpRequestException("Not Redirect Request");
                }
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(message: $"Exception: [{ex}] url: [{URL}]");
                throw;
            }

            return redirectURL;
        }
    }
}
