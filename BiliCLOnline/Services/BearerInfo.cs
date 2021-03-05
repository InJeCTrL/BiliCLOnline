using BiliCLOnline.IServices;
using BiliCLOnline.Models;
using BiliCLOnline.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace BiliCLOnline.Services
{
    public class BearerInfo : IBearerInfo
    {
        public async Task<BearerWrapper> Get(string pattern)
        {
            // 获取评论承载者标识符
            var Id = await Task.Run(()=> Helper.GetFormalIdFromPattern(pattern));
            // 评论承载者标识符格式化正确并且通过验证
            if (Id != string.Empty && await Task.Run(() => Helper.IsValidId(Id)))
            {
                // 获取评论承载者详细信息接口URL
                var DetailAPIURL = Helper.GetBearerDetailAPIURL(Id);
                if (DetailAPIURL != string.Empty)
                {
                    // 针对每种评论承载者做对应处理
                    var Content = await Task.Run(() => WebHelper.GetResponse(DetailAPIURL, "{\"code\":0,"));
                    if (Content != string.Empty)
                    {
                        var top = JsonSerializer.Deserialize<Dictionary<string, object>>(Content);
                        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(top["data"].ToString());
                        switch (Helper.GetBearerTypeById(Id))
                        {
                            case BearerType.Video:
                                var view = JsonSerializer.Deserialize<Dictionary<string, object>>(data["View"].ToString());
                                var BVID = view["bvid"].ToString();
                                var owner = JsonSerializer.Deserialize<Dictionary<string, object>>(view["owner"].ToString());
                                var stat = JsonSerializer.Deserialize<Dictionary<string, object>>(view["stat"].ToString());
                                return new BearerWrapper
                                {
                                    Type = BearerType.Video,
                                    Bearer = new Video
                                    {
                                        CommentCount = int.Parse(stat["reply"].ToString()),
                                        FaceURL = owner["face"].ToString(),
                                        Id = Id,
                                        PubTime = new DateTime(1970, 1, 1, 8, 0, 0, DateTimeKind.Utc).AddSeconds(long.Parse(view["pubdate"].ToString())),
                                        ShareCount = int.Parse(stat["share"].ToString()),
                                        UID = owner["mid"].ToString(),
                                        UName = owner["name"].ToString(),
                                        UserHomeURL = $"https://space.bilibili.com/{owner["mid"]}",
                                        URL = $"https://www.bilibili.com/video/{BVID}",
                                        CoinCount = int.Parse(stat["coin"].ToString()),
                                        CollectCount = int.Parse(stat["favorite"].ToString()),
                                        LikeCount = int.Parse(stat["like"].ToString()),
                                        Title = view["title"].ToString(),
                                        ViewCount = int.Parse(stat["view"].ToString())
                                    }
                                };
                            case BearerType.Article:
                                var stats = JsonSerializer.Deserialize<Dictionary<string, object>>(data["stats"].ToString());
                                return new BearerWrapper
                                {
                                    Type = BearerType.Article,
                                    Bearer = new Article
                                    {
                                        CoinCount = int.Parse(stats["coin"].ToString()),
                                        CollectCount = int.Parse(stats["favorite"].ToString()),
                                        CommentCount = int.Parse(stats["reply"].ToString()),
                                        LikeCount = int.Parse(stats["like"].ToString()),
                                        Id = Id,
                                        ShareCount = int.Parse(stats["share"].ToString()),
                                        Title = data["title"].ToString(),
                                        ViewCount = int.Parse(stats["view"].ToString()),
                                        UID = data["mid"].ToString(),
                                        UName = data["author_name"].ToString(),
                                        UserHomeURL = $"https://space.bilibili.com/{data["mid"]}",
                                        URL = $"https://www.bilibili.com/read/{Id[(Id.IndexOf("|") + 1)..]}",
                                    }
                                };
                            case BearerType.Dynamic:
                                var card = JsonSerializer.Deserialize<Dictionary<string, object>>(data["card"].ToString());
                                var desc = JsonSerializer.Deserialize<Dictionary<string, object>>(card["desc"].ToString());
                                var user_profile = JsonSerializer.Deserialize<Dictionary<string, object>>(desc["user_profile"].ToString());
                                var info = JsonSerializer.Deserialize<Dictionary<string, object>>(user_profile["info"].ToString());
                                return new BearerWrapper
                                {
                                    Type = BearerType.Dynamic,
                                    Bearer = new Dynamic
                                    {
                                        CommentCount = int.Parse(desc["comment"].ToString()),
                                        FaceURL = info["face"].ToString(),
                                        PubTime = new DateTime(1970, 1, 1, 8, 0, 0, DateTimeKind.Utc).AddSeconds(long.Parse(desc["timestamp"].ToString())),
                                        Id = Id,
                                        LikeCount = int.Parse(desc["like"].ToString()),
                                        ShareCount = int.Parse(desc["repost"].ToString()),
                                        UID = desc["uid"].ToString(),
                                        UName = info["uname"].ToString(),
                                        UserHomeURL = $"https://space.bilibili.com/{desc["uid"]}",
                                        URL = $"https://t.bilibili.com/{desc["dynamic_id"]}"
                                    }
                                };
                            default:
                                break;
                        }
                    }
                }
            }
            return new BearerWrapper
            {
                Type = BearerType.Error,
                Bearer = null
            };
        }
    }
}
