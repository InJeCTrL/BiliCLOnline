using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

namespace BiliCLOnline.Utils
{
    public class WebHelper
    {
        /// <summary>
        /// 用于请求代理池的httpclient
        /// </summary>
        private static readonly HttpClient ProxyRequestClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        /// <summary>
        /// 用于请求BilibiliAPI的httpclient
        /// </summary>
        private static HttpClient BiliRequestClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        /// <summary>
        /// 用于获取Bilibili跳转目标链接的httpclient
        /// </summary>
        private static readonly HttpClient BiliJumpRequestClient = new HttpClient(new HttpClientHandler 
        {
            AllowAutoRedirect = false
        })
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        /// <summary>
        /// 代理列表锁: 防止并发地向代理池服务器请求代理列表
        /// </summary>
        private static readonly object ProxyListLock = new object();
        /// <summary>
        /// B站接口请求锁: 防止并发请求B站接口时重复实例化BiliRequestClient
        /// </summary>
        private static readonly object BiliRequestLock = new object();
        private static string ProxyAPIURL = "https://ip.jiangxianli.com/api/proxy_ips?order_by=validated_at&order_rule=DESC";
        /// <summary>
        /// 获取网页响应
        /// </summary>
        /// <param name="URL">目标URL</param>
        /// <param name="BiliAPI">请求目标是否是B站APIURL</param>
        /// <returns>响应文本或string.Empty</returns>
        private static string RawGetResponse(string URL, bool BiliAPI)
        {
            try
            {
                HttpResponseMessage Response;
                if (BiliAPI)
                {
                    Response = BiliRequestClient.GetAsync(URL).Result;
                }
                else
                {
                    // 加锁防止并发访问代理池服务器
                    lock (ProxyListLock)
                    {
                        Response = ProxyRequestClient.GetAsync(URL).Result;
                        Thread.Sleep(500);
                    }
                }
                if (Response.StatusCode == HttpStatusCode.OK)
                {
                    return Response.Content.ReadAsStringAsync().Result;
                }
            }
            catch
            {
                ;
            }
            return string.Empty;
        }
        /// <summary>
        /// 从代理池获取一页代理列表
        /// </summary>
        /// <param name="page">代理页页码</param>
        /// <returns>代理页对应的代理对象列表或空列表</returns>
        private static IList<WebProxy> GetProxyList(int page)
        {
            var ProxyList = new List<WebProxy>();
            // 代理池API首页，用于获取最大页数
            var FirstPageContent = RawGetResponse(ProxyAPIURL, false);
            if (FirstPageContent != string.Empty)
            {
                var FPContentParsed = JsonSerializer.Deserialize<Dictionary<string, object>>(FirstPageContent);
                var FPData = JsonSerializer.Deserialize<Dictionary<string, object>>(FPContentParsed["data"].ToString());
                var LastPage = int.Parse(FPData["last_page"].ToString());
                // 给出的页码有效
                if (page <= LastPage)
                {
                    var PageContent = RawGetResponse(ProxyAPIURL + $"&page={ page }", false);
                    if (PageContent != string.Empty)
                    {
                        var ContentParsed = JsonSerializer.Deserialize<Dictionary<string, object>>(PageContent);
                        var PageData = JsonSerializer.Deserialize<Dictionary<string, object>>(ContentParsed["data"].ToString());
                        var RawProxies = JsonSerializer.Deserialize<IList>(PageData["data"].ToString());
                        foreach (var proxy in RawProxies)
                        {
                            var ProxyParsed = JsonSerializer.Deserialize<Dictionary<string, object>>(proxy.ToString());
                            ProxyList.Add(new WebProxy($"{ ProxyParsed["protocol"] }://{ ProxyParsed["ip"] }:{ ProxyParsed["port"] }"));
                        }
                    }
                }
            }
            return ProxyList;
        }
        /// <summary>
        /// 获取B站API的响应内容直到响应首字符串与ChkPrefix相同
        /// </summary>
        /// <param name="URL">B站APIURL</param>
        /// <param name="ChkPrefix">校验响应内容前缀</param>
        /// <returns>响应文本或stirng.Empty</returns>
        public static string GetResponse(string URL, string ChkPrefix)
        {
            string BiliResponse = string.Empty;
            // 对整个请求过程加锁，防止重复实例化httpclient
            lock (BiliRequestLock)
            {
                BiliResponse = RawGetResponse(URL, true);
                // B站接口返回数据无法通过校验
                if (!BiliResponse.StartsWith(ChkPrefix))
                {
                    #region 先尝试无代理的httpclient
                    BiliRequestClient.Dispose();
                    BiliRequestClient = new HttpClient()
                    {
                        Timeout = TimeSpan.FromSeconds(5)
                    };
                    BiliResponse = RawGetResponse(URL, true);
                    #endregion
                    // 无代理的httpclient无法请求到数据，使用代理池
                    if (!BiliResponse.StartsWith(ChkPrefix))
                    {
                        int Page = 1;
                        while (true)
                        {
                            var WebProxyList = GetProxyList(Page++);
                            // 代理池列表已取完
                            if (WebProxyList.Count == 0)
                            {
                                #region 回头再次尝试无代理的httpclient
                                BiliRequestClient.Dispose();
                                BiliRequestClient = new HttpClient()
                                {
                                    Timeout = TimeSpan.FromSeconds(5)
                                };
                                BiliResponse = RawGetResponse(URL, true);
                                #endregion
                                break;
                            }
                            else
                            {
                                bool AvailableProxy = false;
                                // 对每个代理进行尝试
                                foreach (var proxy in WebProxyList)
                                {
                                    #region 跳过https代理
                                    if (proxy.Address.ToString().StartsWith("https"))
                                    {
                                        continue;
                                    }
                                    #endregion
                                    #region 替换BiliRequestClient
                                    BiliRequestClient.Dispose();
                                    BiliRequestClient = new HttpClient(new HttpClientHandler
                                    {
                                        Proxy = proxy
                                    })
                                    {
                                        Timeout = TimeSpan.FromSeconds(5)
                                    };
                                    #endregion
                                    #region 用替换后的BiliRequestClient请求B站接口
                                    BiliResponse = RawGetResponse(URL, true);
                                    if (BiliResponse.StartsWith(ChkPrefix))
                                    {
                                        AvailableProxy = true;
                                        break;
                                    }
                                    #endregion
                                }
                                if (AvailableProxy)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return BiliResponse;
        }
        /// <summary>
        /// 获取B站分享短链接重定向跳转的目标URL
        /// </summary>
        /// <param name="URL">B站分享短链接</param>
        /// <returns>目标URL或string.Empty</returns>
        public static string GetRedirect(string URL)
        {
            // 循环获取代理列表，循环判断是否可用、Prefix是否正确
            // 完成正确的响应则返回response body
            // 否则返回string.Empty
            int TryTimes = 5;
            while (TryTimes-- > 0)
            {
                try
                {
                    var Response = BiliJumpRequestClient.GetAsync(URL).Result;
                    if (Response.StatusCode == HttpStatusCode.Redirect)
                    {
                        return Response.Headers.Location.ToString();
                    }
                }
                catch
                {
                    ;
                }
            }
            return string.Empty;
        }
    }
}
