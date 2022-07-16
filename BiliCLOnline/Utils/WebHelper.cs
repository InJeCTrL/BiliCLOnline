using BiliCLOnline.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace BiliCLOnline.Utils
{
    public class WebHelper
    {
        /// <summary>
        /// 用于请求BilibiliAPI的httpclient
        /// </summary>
        private readonly HttpClient BiliRequestClient = new();

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

        private readonly ILogger<WebHelper> logger;

        public WebHelper(ILogger<WebHelper> _logger)
        {
            var saKey = Environment.GetEnvironmentVariable("SAKey");

            BiliRequestClient.DefaultRequestHeaders.Add("x-api-key", saKey);
            logger = _logger;
        }

        /// <summary>
        /// 获取B站API的响应JSON
        /// </summary>
        /// <param name="URL">B站APIURL</param>
        /// <param name="ensureSucc">确保返回中code为0</param>
        /// <typeparam name="T">IReturnData</typeparam>
        /// <returns>响应JSON</returns>
        public async Task<BilibiliAPIReturn<T>> GetResponse<T>(string URL, bool ensureSucc = true) where T : ReturnData
        {
            BilibiliAPIReturn<T> responseJSON;

            WebAPIReturn responseWrapper = default;

            URL = $"{Constants.ScrapingAntAPIPrefix}{HttpUtility.UrlEncode(URL)}";

            try
            {
                do
                {
                    using var responseMsg = await BiliRequestClient.GetAsync(URL);
                    responseMsg.EnsureSuccessStatusCode();

                    using var responseStream = await responseMsg.Content.ReadAsStreamAsync();
                    responseWrapper = await JsonSerializer.DeserializeAsync<WebAPIReturn>(responseStream);

                    try
                    {
                        responseJSON = JsonSerializer.Deserialize<BilibiliAPIReturn<T>>(responseWrapper.content);
                    }
                    catch (JsonException)
                    {
                        continue;
                    }

                    if (!ensureSucc || (responseJSON.code == 0))
                    {
                        break;
                    }
                } while (true);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(message: ex.ToString(),
                                args: new object[] { URL });
                throw;
            }
            catch (JsonException ex)
            {
                logger.LogError(message: ex.ToString(),
                                args: new object[] { URL, responseWrapper.content });
                throw;
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
                logger.LogError(message: ex.ToString(),
                                args: new object[] { URL });
                throw;
            }

            return redirectURL;
        }
    }
}
