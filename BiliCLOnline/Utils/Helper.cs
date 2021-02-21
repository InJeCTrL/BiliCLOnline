using BiliCLOnline.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        /// 获取评论承载者标识符前缀与本体
        /// </summary>
        /// <param name="Id">评论承载者标识符</param>
        /// <returns></returns>
        public static string[] GetIdHeadBody(string Id)
        {
            var PosSplit = Id.IndexOf('|');
            if (PosSplit == -1)
            {
                return null;
            }
            return new string[] { Id.Substring(0, PosSplit), Id.Substring(PosSplit) };
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
            var parts = GetIdHeadBody(Id);
            if (parts == null)
            {
                return string.Empty;
            }
            // 前缀
            var head = parts[0];
            // 真实Id
            var body = parts[1];
            if (head == "aid" || head == "bvid")
            {
                return $"https://www.bilibili.com/video/{body}#reply";
            }
            else if (head == "cv")
            {
                return $"https://www.bilibili.com/read/{body}#reply";
            }
            else if (head == "did")
            {
                return $"https://t.bilibili.com/{body}#reply";
            }
            else
            {
                return string.Empty;
            }
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
            return content.StartsWith("{\"code\":0,");
        }
        /// <summary>
        /// 获取Get请求的响应body
        /// </summary>
        /// <param name="URL">请求URL</param>
        /// <returns></returns>
        public static string GetResponse(string URL)
        {
            string ret = string.Empty;
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
                ;
            }
            finally
            {
                if (webRequest != null)
                {
                    webRequest.Abort();
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
    }
}
