using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BiliCLOnline.Models;
using Microsoft.AspNetCore.Http;
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
        /// 获取Bilibili登录用二维码
        /// </summary>
        /// <returns><二维码内容, Bilibili登录标识></returns>
        public async Task<Tuple<string, string>> GetBilibiliLoginQRCode()
        {
            try
            {
                using var response = await BiliRequestClient.GetAsync(LoginQRCodeAPI);
                response.EnsureSuccessStatusCode();
                var loginGenerateData = await response.Content.ReadFromJsonAsync<BilibiliAPIReturn<LoginGenerateData>>();
                if (loginGenerateData.code != 0)
                {
                    return Tuple.Create(string.Empty, string.Empty);
                }

                return Tuple.Create(loginGenerateData.data.url, loginGenerateData.data.qrcode_key);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is JsonException)
            {
                logger.LogError(message: ex.ToString());
            }

            return Tuple.Create(string.Empty, string.Empty);
        }

        /// <summary>
        /// 校验Bilibili登录状态
        /// </summary>
        /// <param name="key">Bilibili登录标识</param>
        /// <returns><是否通过验证, Cookie></returns>
        public async Task<Tuple<bool, string>> VerifyBilibiliLogin(string key)
        {
            try
            {
                using var response = await BiliRequestClient.GetAsync(string.Format(LoginCheckAPITemplate, key));
                response.EnsureSuccessStatusCode();

                var loginCheckData = await response.Content.ReadFromJsonAsync<BilibiliAPIReturn<LoginCheckData>>();
                if (loginCheckData.data.code != 0)
                {
                    return Tuple.Create(false, string.Empty);
                }

                var cookie = loginCheckData.data.url.Replace("&", ";");
                cookie = cookie[(cookie.IndexOf("?") + 1)..];

                return Tuple.Create(true, cookie);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is JsonException)
            {
                logger.LogError(message: ex.ToString());
            }

            return Tuple.Create(false, string.Empty);
        }

        /// <summary>
        /// 获取B站API的响应JSON
        /// </summary>
        /// <param name="URL">B站APIURL</param>
        /// <typeparam name="T">IReturnData</typeparam>
        /// <returns>响应JSON</returns>
        public async Task<BilibiliAPIReturn<T>> GetResponse<T>(string URL, string cookie) where T : ReturnData
        {
            BilibiliAPIReturn<T> responseJSON = default;

            await ConcurrentLimit.WaitAsync();

            try
            {
                for (int tryTime = 0; tryTime < MaxTryCount; ++tryTime)
                {
                    try
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Get, URL);
                        if (!string.IsNullOrEmpty(cookie))
                        {
                            request.Headers.Add(
                                "Cookie",
                                Encoding.ASCII.GetString(Encoding.UTF8.GetBytes(cookie)));
                        }

                        using var responseMsg = await BiliRequestClient.SendAsync(request);
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
