using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using BiliCLOnline.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static BiliCLOnline.Utils.Constants;

namespace BiliCLOnline.Utils
{
    public class WebHelper
    {
        /// <summary>
        /// 用于请求BilibiliAPI的httpclient
        /// </summary>
        private readonly HttpClient BiliRequestClient = new HttpClient
        {
            DefaultRequestHeaders =
            {
                UserAgent =
                {
                    new ProductInfoHeaderValue("Mozilla", "5.0")
                }
            }
        };

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

        private readonly ILogger<WebHelper> logger;

        public WebHelper(ILogger<WebHelper> _logger, IConfiguration _config)
        {
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

            await ConcurrentLimit.WaitAsync();

            try
            {
                for (int tryTime = 0; tryTime < MaxTryCount; ++tryTime)
                {
                    try
                    {
                        using var responseMsg = await BiliRequestClient.GetAsync(URL);
                        responseMsg.EnsureSuccessStatusCode();
                        responseJSON = await responseMsg.Content.ReadFromJsonAsync<BilibiliAPIReturn<T>>();

                        if (responseJSON.code != 0)
                        {
                            throw new HttpRequestException(URL);
                        }

                        break;
                    }
                    catch (Exception ex) when (ex is InvalidOperationException || ex is HttpRequestException || ex is JsonException || ex is TaskCanceledException)
                    {
                        logger.LogError(message: $"Exception: [{ex}] url: [{URL}]");
                        if (tryTime == MaxTryCount)
                        {
                            logger.LogError(message: $"Exception: Http request try time exceeds, url: [{URL}]");
                        }
                    }
                }
            }
            finally
            {
                ConcurrentLimit.Release();
            }

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
