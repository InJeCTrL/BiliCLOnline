using BiliCLOnline.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

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
        /// <summary>
        /// 根据评论承载者标识符获得评论承载者类型
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public static BearerType GetBearerTypeById(string Id)
        {
            var Parts = GetIdHeadBody(Id);
            var Btype = BearerType.Error;
            if (Parts == null)
            {
                return Btype;
            }
            switch (Parts[0])
            {
                case "aid":
                case "bvid":
                    Btype = BearerType.Video;
                    break;
                case "cv":
                    Btype = BearerType.Article;
                    break;
                case "did":
                    Btype = BearerType.Dynamic;
                    break;
                default:
                    break;
            }
            return Btype;
        }
        /// <summary>
        /// 获取评论承载者标识符前缀与本体
        /// </summary>
        /// <param name="Id">评论承载者标识符</param>
        /// <returns>string[2]或null</returns>
        public static string[] GetIdHeadBody(string Id)
        {
            var PosSplit = Id.IndexOf('|');
            if (PosSplit == -1 || PosSplit + 1 >= Id.Length)
            {
                return null;
            }
            return new string[] { Id[0..PosSplit], Id[(PosSplit + 1)..] };
        }
        /// <summary>
        /// 检查评论承载者标识符是否符合规定
        /// </summary>
        /// <param name="Id">评论承载者标准标识符</param>
        /// <returns>格式化正确: true, 格式化错误: false</returns>
        public static Task<bool> CheckIdHead(string Id)
        {
            var parts = GetIdHeadBody(Id);
            if (parts != null && parts.Length == 2)
            {
                var head = parts[0];
                if (head == "aid" || head == "bvid" ||
                    head == "cv" || head == "did")
                {
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }
        /// <summary>
        /// 根据评论承载者标识符获取评论承载者信息接口URL
        /// </summary>
        /// <param name="Id">评论承载者标识符</param>
        /// <returns>评论承载者信息接口URL或string.Empty</returns>
        public static string GetInfoAPIURL(string Id)
        {
            var parts = GetIdHeadBody(Id);
            if (parts != null && parts.Length == 2)
            {
                // 前缀
                var head = parts[0];
                // 真实Id
                var body = parts[1];
                string InfoAPIURL = head switch
                {
                    "aid" => $"http://api.bilibili.com/x/web-interface/archive/stat?aid={body}",
                    "bvid" => $"http://api.bilibili.com/x/web-interface/archive/stat?bvid={body}",
                    "cv" => $"http://api.bilibili.com/x/article/viewinfo?id={body}",
                    "did" => $"http://api.vc.bilibili.com/dynamic_svr/v1/dynamic_svr/get_dynamic_detail?dynamic_id={body}",
                    _ => string.Empty,
                };
                return InfoAPIURL;
            }
            return string.Empty;
        }
        /// <summary>
        /// 根据评论承载者标识符获取评论承载者详细信息接口URL
        /// </summary>
        /// <param name="Id">评论承载者标识符</param>
        /// <returns>评论承载者详细信息接口URL或string.Empty</returns>
        public static string GetBearerDetailAPIURL(string Id)
        {
            var parts = GetIdHeadBody(Id);
            if (parts != null && parts.Length == 2)
            {
                // 前缀
                var head = parts[0];
                // 真实Id
                var body = parts[1];
                string InfoAPIURL = head switch
                {
                    "aid" => $"http://api.bilibili.com/x/web-interface/view/detail?aid={body}",
                    "bvid" => $"http://api.bilibili.com/x/web-interface/view/detail?bvid={body}",
                    "cv" => $"http://api.bilibili.com/x/article/viewinfo?id={body}",
                    "did" => $"http://api.vc.bilibili.com/dynamic_svr/v1/dynamic_svr/get_dynamic_detail?dynamic_id={body}",
                    _ => string.Empty,
                };
                return InfoAPIURL;
            }
            return string.Empty;
        }
        /// <summary>
        /// 获取评论区信息接口URL
        /// </summary>
        /// <param name="Id">评论承载者标识符</param>
        /// <returns>评论区信息接口URL或string.Empty</returns>
        public static string GetReplyAPIURL(string Id)
        {
            var parts = GetIdHeadBody(Id);
            if (parts != null && parts.Length == 2)
            {
                // 前缀
                var head = parts[0];
                // 真实Id
                var body = parts[1];
                string OID = body;
                int WorkType;
                if (head == "aid" || head == "bvid")
                {
                    WorkType = 1;
                }
                else if (head == "cv")
                {
                    WorkType = 12;
                    return $"http://api.bilibili.com/x/v2/reply?oid={OID}&type={WorkType}&sort=1&ps=49&pn=";
                }
                else if (head == "did")
                {
                    WorkType = 17;
                }
                else
                {
                    return string.Empty;
                }
                var InfoAPIURL = GetInfoAPIURL(Id);
                if (InfoAPIURL != string.Empty)
                {
                    var Content = WebHelper.GetResponse(InfoAPIURL, "{\"code\":0,");
                    if (Content != string.Empty)
                    {
                        var top = JsonSerializer.Deserialize<Dictionary<string, object>>(Content);
                        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(top["data"].ToString());
                        // 动态需要判断是否存在rid
                        if (WorkType == 17 && data.ContainsKey("card"))
                        {
                            var card = JsonSerializer.Deserialize<Dictionary<string, object>>(data["card"].ToString());
                            var desc = JsonSerializer.Deserialize<Dictionary<string, object>>(card["desc"].ToString());
                            // get_dynamic_detail中type为4: WorkType=17, OID=动态ID
                            // get_dynamic_detail中type为2: WorkType=11, OID=rid
                            if (desc["type"].ToString() == "2")
                            {
                                WorkType = 11;
                                OID = desc["rid"].ToString();
                            }
                        }
                        // 视频稿件使用aid作为oid
                        else if (WorkType == 1)
                        {
                            OID = data["aid"].ToString();
                        }
                        return $"http://api.bilibili.com/x/v2/reply?oid={OID}&type={WorkType}&sort=1&ps=49&pn=";
                    }
                }
            }
            return string.Empty;
        }
        /// <summary>
        /// 评论条目URL
        /// </summary>
        /// <param name="Id">评论承载者标识符</param>
        /// <returns>评论条目URL或string.Empty</returns>
        public static string GetReplyURL(string Id)
        {
            var parts = GetIdHeadBody(Id);
            if (parts != null && parts.Length == 2)
            {
                // 前缀
                var head = parts[0];
                // 真实Id
                var body = parts[1];
                string ReplyURL = head switch
                {
                    "aid" => $"http://www.bilibili.com/video/av{body}#reply",
                    "bvid" => $"http://www.bilibili.com/video/BV{body}#reply",
                    "cv" => $"http://www.bilibili.com/read/cv{body}#reply",
                    "did" => $"http://t.bilibili.com/{body}#reply",
                    _ => string.Empty,
                };
                return ReplyURL;
            }
            return string.Empty;
        }
        /// <summary>
        /// 检查评论承载者Id是否对应有效的媒体稿件/动态
        /// </summary>
        /// <param name="Id">评论承载者Id</param>
        /// <returns>评论承载者Id有效: true, 评论承载者Id无效: false</returns>
        public static bool IsValidId(string Id)
        {
            var InfoAPIURL = GetInfoAPIURL(Id);
            if (InfoAPIURL != string.Empty)
            {
                var Content = WebHelper.GetResponse(InfoAPIURL, "{\"code\":");
                if (Content != string.Empty && Content.StartsWith("{\"code\":0,"))
                {
                    var top = JsonSerializer.Deserialize<Dictionary<string, object>>(Content);
                    if (top.ContainsKey("data"))
                    {
                        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(top["data"].ToString());
                        if (data.Count > 1)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 获取随机抽选结果列表
        /// </summary>
        /// <param name="Source">原始列表</param>
        /// <param name="Count">抽选个数</param>
        public static List<int> GetRandomIdxList(List<int> Source, int Count)
        {
            var random = new Random(Guid.NewGuid().GetHashCode());
            int sourceLen = Source.Count;
            var result = new List<int>();
            for (int i = sourceLen; i > sourceLen - Count; --i)
            {
                int pRandom = random.Next(0, i);
                result.Add(Source.ElementAt(pRandom));
                Source[pRandom] = Source[i - 1];
            }
            return result;
        }
        /// <summary>
        /// 获取B站分享短链接指向的目标URL
        /// </summary>
        /// <param name="ShareURL">分享短链接URL</param>
        /// <returns>目标作品URL或string.Empty</returns>
        public static string GetRealURL(string ShareURL)
        {
            var RealURL = WebHelper.GetRedirect(ShareURL);
            if (RealURL != string.Empty && !RealURL.Contains("b23.tv"))
            {
                return RealURL;
            }
            return string.Empty;
        }
        /// <summary>
        /// 根据ID获取格式化的评论承载者标准标识符
        /// </summary>
        /// <param name="RawId">av号/bv号等</param>
        /// <returns>格式化但未联网验证有效性的评论承载者标准标识符 或 string.Empty</returns>
        public static string GetFormalIdFromRawId(string RawId)
        {
            if (RawId.Length > 0)
            {
                // Id由数字开头: 动态
                if (RawId[0] >= '0' && RawId[0] <= '9')
                {
                    return $"did|{RawId}";
                }
                // 其他作品
                else if (RawId.Length >= 2)
                {
                    var Prefix = RawId[..2].ToLower();
                    var Body = RawId[2..];
                    return Prefix switch
                    {
                        "av" => $"aid|{Body}",
                        "bv" => $"bvid|{Body}",
                        "cv" => $"cv|{Body}",
                        _ => string.Empty,
                    };
                }
            }
            return string.Empty;
        }
        /// <summary>
        /// 根据ID或URL获取评论承载者标识符
        /// </summary>
        /// <param name="pattern">作品RawID或URL</param>
        /// <returns>格式化的评论承载者标识符 或 string.Empty</returns>
        public static string GetFormalIdFromPattern(string pattern)
        {
            if (pattern.Length > 0)
            {
                // 去除可能包含的锚定
                if (pattern.Contains('#'))
                {
                    pattern = pattern[..pattern.IndexOf('#')];
                }
                // URL
                if (pattern.StartsWith("http"))
                {
                    // 需要解析跳转
                    if (pattern.Contains("b23.tv"))
                    {
                        var RealURL = GetRealURL(pattern);
                        if (RealURL != string.Empty)
                        {
                            return GetFormalIdFromPattern(RealURL);
                        }
                    }
                    // 只需提取RawId
                    else
                    {
                        var Lower = pattern.ToLower();
                        string RawId;
                        if (Lower.Contains("/av"))
                        {
                            RawId = pattern[(Lower.IndexOf("/av") + 1)..];
                        }
                        else if (Lower.Contains("/bv"))
                        {
                            RawId = pattern[(Lower.IndexOf("/bv") + 1)..];
                        }
                        else if (Lower.Contains("/cv"))
                        {
                            RawId = pattern[(Lower.IndexOf("/cv") + 1)..];
                        }
                        else if (Lower.Contains("t.bilibili.com/"))
                        {
                            RawId = pattern[(Lower.IndexOf("t.bilibili.com/") + 15)..];
                        }
                        else
                        {
                            RawId = string.Empty;
                        }
                        // 去除后续参数
                        if (RawId.Contains('?'))
                        {
                            RawId = RawId[..RawId.IndexOf('?')];
                        }
                        // 去除子级别路径
                        if (RawId.Contains('/'))
                        {
                            RawId = RawId[..RawId.IndexOf('/')];
                        }
                        if (RawId.Length > 0)
                        {
                            return GetFormalIdFromRawId(RawId);
                        }
                    }
                }
                // RawID
                else
                {
                    return GetFormalIdFromRawId(pattern);
                }
            }
            return string.Empty;
        }
    }
}
