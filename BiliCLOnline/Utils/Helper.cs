using BiliCLOnline.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;

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
        /// <returns></returns>
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
        /// 检查评论承载者标识符是否合法
        /// </summary>
        /// <param name="Id">评论承载者标准标识符</param>
        /// <returns></returns>
        public static bool CheckIdHead(string Id)
        {
            var parts = GetIdHeadBody(Id);
            if (parts == null)
            {
                return false;
            }
            var head = parts[0];
            if (head == "aid" || head == "bvid" ||
                head == "cv" || head == "did")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 根据评论承载者标识符获取评论承载者信息接口URL
        /// </summary>
        /// <param name="Id">评论承载者标识符</param>
        /// <returns></returns>
        public static string GetInfoAPIURL(string Id)
        {
            var parts = GetIdHeadBody(Id);
            if (parts == null)
            {
                return string.Empty;
            }
            // 前缀
            var head = parts[0];
            // 真实Id
            var body = parts[1];
            // 作品信息接口地址
            string InfoAPIURL;
            switch (head)
            {
                case "aid":
                    InfoAPIURL = $"https://api.bilibili.com/x/web-interface/archive/stat?aid={body}";
                    break;
                case "bvid":
                    InfoAPIURL = $"https://api.bilibili.com/x/web-interface/archive/stat?bvid={body}";
                    break;
                case "cv":
                    InfoAPIURL = $"https://api.bilibili.com/x/article/viewinfo?id={body}";
                    break;
                case "did":
                    InfoAPIURL = $"https://api.vc.bilibili.com/dynamic_svr/v1/dynamic_svr/get_dynamic_detail?dynamic_id={body}";
                    break;
                default:
                    InfoAPIURL = "";
                    break;
            }
            return InfoAPIURL;
        }
        /// <summary>
        /// 根据评论承载者标识符获取评论承载者详细信息接口URL
        /// </summary>
        /// <param name="Id">评论承载者标识符</param>
        /// <returns></returns>
        public static string GetBearerDetailAPIURL(string Id)
        {
            var parts = GetIdHeadBody(Id);
            if (parts == null)
            {
                return string.Empty;
            }
            // 前缀
            var head = parts[0];
            // 真实Id
            var body = parts[1];
            // 作品信息接口地址
            string InfoAPIURL;
            switch (head)
            {
                case "aid":
                    InfoAPIURL = $"https://api.bilibili.com/x/web-interface/view/detail?aid={body}";
                    break;
                case "bvid":
                    InfoAPIURL = $"https://api.bilibili.com/x/web-interface/view/detail?bvid={body}";
                    break;
                case "cv":
                    InfoAPIURL = $"https://api.bilibili.com/x/article/viewinfo?id={body}";
                    break;
                case "did":
                    InfoAPIURL = $"https://api.vc.bilibili.com/dynamic_svr/v1/dynamic_svr/get_dynamic_detail?dynamic_id={body}";
                    break;
                default:
                    InfoAPIURL = "";
                    break;
            }
            return InfoAPIURL;
        }
        /// <summary>
        /// 获取评论区信息接口URL
        /// </summary>
        /// <param name="Id">评论承载者标识符</param>
        /// <returns></returns>
        public static string GetReplyAPIURL(string Id)
        {
            var parts = GetIdHeadBody(Id);
            if (parts == null)
            {
                return string.Empty;
            }
            // 前缀
            var head = parts[0];
            // 真实Id
            var body = parts[1];
            int WorkType = 0;
            string OID = body;
            if (head == "aid" || head == "bvid")
            {
                WorkType = 1;
            }
            else if (head == "cv")
            {
                WorkType = 12;
                return $"https://api.bilibili.com/x/v2/reply?oid={OID}&type={WorkType}&sort=0&pn=";
            }
            else if (head == "did")
            {
                WorkType = 17;
            }
            else
            {
                return string.Empty;
            }
            var content = GetResponse(GetInfoAPIURL(Id));
            var top = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
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
            return $"https://api.bilibili.com/x/v2/reply?oid={OID}&type={WorkType}&sort=0&pn=";
        }
        /// <summary>
        /// 评论条目URL
        /// </summary>
        /// <param name="Id">评论承载者标识符</param>
        /// <returns></returns>
        public static string GetReplyURL(string Id)
        {
            var ret = string.Empty;
            var parts = GetIdHeadBody(Id);
            if (parts == null)
            {
                return ret;
            }
            // 前缀
            var head = parts[0];
            // 真实Id
            var body = parts[1];
            switch (head)
            {
                case "aid":
                    ret = $"https://www.bilibili.com/video/av{body}#reply";
                    break;
                case "bvid":
                    ret = $"https://www.bilibili.com/video/BV{body}#reply";
                    break;
                case "cv":
                    ret = $"https://www.bilibili.com/read/cv{body}#reply";
                    break;
                case "did":
                    ret = $"https://t.bilibili.com/{body}#reply";
                    break;
                default:
                    break;
            }
            return ret;
        }
        /// <summary>
        /// 检查评论承载者Id是否对应有效的媒体稿件/动态
        /// </summary>
        /// <param name="Id">评论承载者Id</param>
        /// <returns></returns>
        public static bool IsValidId(string Id)
        {
            var InfoAPIURL = GetInfoAPIURL(Id);
            var content = GetResponse(InfoAPIURL);
            if (!content.StartsWith("{\"code\":0,"))
            {
                return false;
            }
            var top = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            if (!top.ContainsKey("data"))
            {
                return false;
            }
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(top["data"].ToString());
            if (data.Count() == 1 && data.ContainsKey("_gt_"))
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 获取Get请求的响应body
        /// </summary>
        /// <param name="URL">请求URL</param>
        /// <returns></returns>
        public static string GetResponse(string URL)
        {
            string ret = string.Empty;
            int TryTime = 5;
            while (TryTime > 0)
            {
                bool Success = true;
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(URL);
                try
                {
                    using (WebResponse webResponse = webRequest.GetResponse())
                    {
                        using (Stream respstream = webResponse.GetResponseStream())
                        {
                            using (StreamReader streamReader = new StreamReader(respstream))
                            {
                                ret = streamReader.ReadToEnd();
                            }
                        }
                    }
                }
                catch
                {
                    Success = false;
                }
                finally
                {
                    if (webRequest != null)
                    {
                        webRequest.Abort();
                    }
                    --TryTime;
                }
                if (Success)
                {
                    break;
                }
            }
            return ret;
        }
        /// <summary>
        /// 获取随机抽选结果列表
        /// </summary>
        /// <param name="To">结果列表</param>
        /// <param name="From">原始列表</param>
        /// <param name="Count">抽选个数</param>
        public static void GetRandomResultList(List<Reply> To, List<Reply> From, int Count)
        {
            Random random = new Random();
            int FromLen = From.Count();
            for (int i = FromLen; i > FromLen - Count; --i)
            {
                int pRandom = random.Next(0, i);
                To.Add(From.ElementAt(pRandom));
                From[pRandom] = From[i - 1];
            }
        }
        public static string GetRealURL(string ShareURL)
        {
            string ret = string.Empty;
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(ShareURL);
            webRequest.AllowAutoRedirect = false;
            try
            {
                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    ret = webResponse.Headers["Location"];
                }
            }
            catch
            {
                ;
            }
            finally
            {
                if (webRequest != null)
                {
                    webRequest.Abort();
                }
            }
            if (ret == null || ret == string.Empty || ret.Length == 0 || ret.Contains("b23.tv"))
            {
                return string.Empty;
            }
            return ret;
        }
        /// <summary>
        /// 根据ID获取评论承载者标识符
        /// </summary>
        /// <param name="RawId"></param>
        /// <returns></returns>
        public static string GetFormalIdFromRawId(string RawId)
        {
            string Id = string.Empty;
            if (RawId.Length < 3)
            {
                // 动态
                if (RawId[0] >= '0' && RawId[0] <= '9')
                {
                    Id = $"did|{RawId}";
                }
            }
            else
            {
                var Prefix = RawId[0..2].ToLower();
                var Body = RawId[2..];
                switch (Prefix)
                {
                    case "av":
                        Id = $"aid|{Body}";
                        break;
                    case "bv":
                        Id = $"bvid|{Body}";
                        break;
                    case "cv":
                        Id = $"cv|{Body}";
                        break;
                    default:
                        Id = $"did|{RawId}";
                        break;
                }
            }
            return Id;
        }
        /// <summary>
        /// 根据ID或URL获取评论承载者标识符
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static string GetFormalIdFromPattern(string pattern)
        {
            string RawId = string.Empty;
            if (pattern == null || pattern == string.Empty || pattern.Length == 0)
            {
                return RawId;
            }
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
                    return GetFormalIdFromPattern(GetRealURL(pattern));
                }
                // 只需提取RawId
                else
                {
                    var Lower = pattern.ToLower();
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
                    else if (Lower.Contains("t.bilibili.com/") &&
                        Lower.IndexOf("t.bilibili.com/") + 15 < Lower.Length)
                    {
                        RawId = pattern[(Lower.IndexOf("t.bilibili.com/") + 15)..];
                    }
                    if (RawId.Contains('?'))
                    {
                        RawId = RawId[..RawId.IndexOf('?')];
                    }
                    if (RawId.Contains('/'))
                    {
                        RawId = RawId[..RawId.IndexOf('/')];
                    }
                }
            }
            // RawID
            else
            {
                RawId = pattern;
            }
            return GetFormalIdFromRawId(RawId);
        }
    }
}
