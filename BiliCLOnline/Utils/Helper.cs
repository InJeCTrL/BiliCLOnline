using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using BiliCLOnline.Models;
using Microsoft.Extensions.Logging;
using static BiliCLOnline.Utils.Constants;

namespace BiliCLOnline.Utils
{
    public enum BearerType
    {
        Error = -1,
        Video,
        Article,
        Dynamic
    }
    public class Helper
    {
        private readonly ILogger<Helper> logger;

        private readonly WebHelper webHelper;

        // <GUID, <Completed, Status, Replies>>
        public readonly ConcurrentDictionary<string, Tuple<bool, string, List<Reply>>> guidReplyResults;

        public Helper(ILogger<Helper> _logger, WebHelper _webHelper)
        {
            logger = _logger;
            webHelper = _webHelper;
            guidReplyResults = new();
        }

        /// <summary>
        /// 获取评论承载者基本要素
        /// </summary>
        /// <param name="formalId">评论承载者标识符</param>
        /// <returns>评论承载者类型, 作品Id前缀, 去除前缀的作品Id</returns>
        public Tuple<BearerType, string, string> GetBearerBasics(string formalId)
        {
            var parts = formalId.Split('|');

            Tuple<BearerType, string, string> basics = parts[0] switch
            {
                "aid" or "bvid" => Tuple.Create(BearerType.Video, parts[0], parts[1]),
                "cv" => Tuple.Create(BearerType.Article, parts[0], parts[1]),
                "did" => Tuple.Create(BearerType.Dynamic, parts[0], parts[1]),
                _ => Tuple.Create(BearerType.Error, parts[0], parts[1]),
            };

            return basics;
        }

        /// <summary>
        /// 检查评论承载者标识符是否符合基本语法
        /// </summary>
        /// <param name="formalId">评论承载者标准标识符</param>
        /// <returns>格式正确: true, 格式错误: false</returns>
        public bool CheckIdSyntax(string formalId)
        {
            return !string.IsNullOrEmpty(formalId)
                && ((formalId.StartsWith("aid|") && formalId.Length > 4)
                    || (formalId.StartsWith("bvid|") && formalId.Length > 5)
                    || (formalId.StartsWith("cv|") && formalId.Length > 3)
                    || (formalId.StartsWith("did|") && formalId.Length > 4));
        }

        /// <summary>
        /// 根据作品Id前缀获取评论承载者详细信息接口URL
        /// </summary>
        /// <param name="idPrefix">作品Id前缀</param>
        /// <param name="idBody">作品Id主体</param>
        /// <returns>评论承载者详细信息接口URL或string.Empty</returns>
        public string GetBearerDetailAPIURL(string idPrefix, string idBody)
        {
            return idPrefix switch
            {
                "aid" => string.Format(BearerDetailAPITemplate.AID, idBody),
                "bvid" => string.Format(BearerDetailAPITemplate.BVID, idBody),
                "cv" => string.Format(BearerDetailAPITemplate.CV, idBody),
                "did" => string.Format(BearerDetailAPITemplate.DID, idBody),
                _ => string.Empty,
            };
        }

        /// <summary>
        /// 评论条目URL前缀
        /// </summary>
        /// <param name="idPrefix">作品Id前缀</param>
        /// <param name="idBody">作品Id主体</param>
        /// <returns>评论条目URL前缀或string.Empty</returns>
        public string GetReplyURLPrefix(string idPrefix, string idBody)
        {
            return idPrefix switch
            {
                "aid" => string.Format(ReplyURLPrefixTemplate.AID, idBody),
                "bvid" => string.Format(ReplyURLPrefixTemplate.BVID, idBody),
                "cv" => string.Format(ReplyURLPrefixTemplate.CV, idBody),
                "did" => string.Format(ReplyURLPrefixTemplate.DID, idBody),
                _ => string.Empty,
            };
        }

        /// <summary>
        /// 获取评论区信息接口URL
        /// </summary>
        /// <param name="idPrefix">作品Id前缀</param>
        /// <param name="idBody">作品Id主体</param>
        /// <param name="detailData">作品数据</param>
        /// <returns>评论区信息接口URL或string.Empty</returns>
        public string GetReplyAPIURL(string idPrefix, string idBody, DetailData detailData)
        {
            var oid = idBody;

            int workType = idPrefix switch
            {
                "aid" or "bvid" => 1,
                "cv" => 12,
                "did" => 17,
                _ => throw new ArgumentException(null, nameof(idPrefix)),
            };

            // 动态需要判断是否存在rid
            if (workType == 17)
            {
                var dynamicDetail = (DynamicDetailData)detailData;

                // get_dynamic_detail中type为4: WorkType=17, OID=动态ID
                // get_dynamic_detail中type为2: WorkType=11, OID=rid
                if (dynamicDetail.item.basic.comment_type == 11)
                {
                    workType = 11;
                    oid = dynamicDetail.item.basic.rid_str.ToString();
                }
            }
            // 视频稿件使用aid作为oid
            else if (workType == 1)
            {
                var videoDetail = (VideoDetailData)detailData;

                oid = videoDetail.View.aid.ToString();
            }

            return string.Format(ReplyAPITemplate, oid, workType, ReplyPageSize);
        }

        /// <summary>
        /// 检查评论承载者是否对应有效的媒体稿件/动态
        /// </summary>
        /// <param name="detailAPI">详细信息API</param>
        /// <typeparam name="T">ReturnData</typeparam>
        /// <returns>评论承载者有效: true, ReturnData; 评论承载者Id无效: false, default</returns>
        public async Task<Tuple<bool, DetailData>> IsValidWork<T>(string detailAPI) where T : DetailData
        {
            BilibiliAPIReturn<T> apiReturn;

            try
            {
                apiReturn = await webHelper.GetResponse<T>(detailAPI, "");
            }
            catch (JsonException ex)
            {
                logger.LogError(message: ex.ToString(),
                    args: new object[] { detailAPI });

                return Tuple.Create(false, default(DetailData));
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(message: ex.ToString(),
                    args: new object[] { detailAPI });

                return Tuple.Create(false, default(DetailData));
            }

            if (apiReturn.code != 0)
            {
                return Tuple.Create(false, default(DetailData));
            }

            return Tuple.Create(true, (DetailData)apiReturn.data);
        }

        /// <summary>
        /// 获取B站分享短链接指向的目标URL
        /// </summary>
        /// <param name="shareURL">分享短链接URL</param>
        /// <returns>目标作品URL或string.Empty</returns>
        private async Task<string> GetRealURL(string shareURL)
        {
            string realURL = string.Empty;

            try
            {
                realURL = await webHelper.GetRedirect(shareURL);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(message: ex.ToString(),
                    args: new object[] { shareURL });
            }

            return realURL;
        }

        /// <summary>
        /// 根据ID或URL获取评论承载者标识符
        /// </summary>
        /// <param name="pattern">作品RawID或URL</param>
        /// <returns>格式化的评论承载者标识符 或 string.Empty</returns>
        public async Task<string> GetFormalIdFromPattern(string pattern)
        {
            string bearerId = string.Empty;

            #region 去除可能包含的锚定
            if (pattern.Contains('#'))
            {
                pattern = pattern[..pattern.IndexOf('#')];
            }
            #endregion

            string rawId = string.Empty;

            #region 根据URL获取rawId
            if (pattern.Contains("http:") || pattern.Contains("https:"))
            {
                pattern = pattern[pattern.IndexOf("http")..];

                #region 解析短分享链接为常规URL
                if (pattern.Contains("b23.tv"))
                {
                    pattern = await GetRealURL(pattern);
                }
                #endregion

                #region 从URL提取rawId
                if (!string.IsNullOrEmpty(pattern))
                {
                    var lower = pattern.ToLower();

                    if (lower.Contains("/av"))
                    {
                        rawId = pattern[(lower.IndexOf("/av") + 1)..];
                    }
                    else if (lower.Contains("/bv"))
                    {
                        rawId = pattern[(lower.IndexOf("/bv") + 1)..];
                    }
                    else if (lower.Contains("/cv"))
                    {
                        rawId = pattern[(lower.IndexOf("/cv") + 1)..];
                    }
                    else if (lower.Contains("t.bilibili.com/"))
                    {
                        rawId = pattern[(lower.IndexOf("t.bilibili.com/") + 15)..];
                    }
                    else if (lower.Contains("m.bilibili.com/dynamic/"))
                    {
                        rawId = pattern[(lower.IndexOf("m.bilibili.com/dynamic/") + 23)..];
                    }
                    else if (lower.Contains("bilibili.com/opus/"))
                    {
                        rawId = pattern[(lower.IndexOf("bilibili.com/opus/") + 18)..];
                    }

                    #region 去除后续参数
                    if (rawId.Contains('?'))
                    {
                        rawId = rawId[..rawId.IndexOf('?')];
                    }
                    #endregion

                    #region 去除子级别路径
                    if (rawId.Contains('/'))
                    {
                        rawId = rawId[..rawId.IndexOf('/')];
                    }
                    #endregion
                }
                #endregion
            }
            else
            {
                rawId = pattern;
            }
            #endregion

            #region 根据rawId获取格式化的评论承载者标准标识符(未联网验证formal Id)
            if (!string.IsNullOrEmpty(rawId))
            {
                #region Id由数字开头: 动态
                if (rawId[0] >= '0' && rawId[0] <= '9')
                {
                    bearerId = $"did|{rawId}";
                }
                #endregion
                #region 其他作品
                else if (rawId.Length >= 2)
                {
                    var prefix = rawId[..2].ToLower();

                    var body = rawId[2..];

                    bearerId = prefix switch
                    {
                        "av" => $"aid|{body}",
                        "bv" => $"bvid|{body}",
                        "cv" => $"cv|{body}",
                        _ => string.Empty,
                    };
                }
                #endregion
            }
            #endregion

            return bearerId;
        }

        /// <summary>
        /// B站返回时间戳转换为DateTime
        /// </summary>
        /// <param name="timestamp">时间戳</param>
        /// <returns>DateTime</returns>
        public DateTime TimeTrans(long timestamp) => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);

        /// <summary>
        /// 调用默认浏览器打开链接
        /// </summary>
        /// <param name="url">网址链接</param>
        public static void OpenURL(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
