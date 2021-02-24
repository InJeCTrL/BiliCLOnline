using BiliCLOnline.IServices;
using BiliCLOnline.Models;
using BiliCLOnline.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BiliCLOnline.Services
{
    public class LotteryResult : ILotteryResult
    {
        public async Task<IEnumerable<Reply>> GetList(
            string id, int Count, bool UnlimitedStart, bool UnlimitedEnd, 
            DateTime Start, DateTime End, bool GETStart, bool LETEnd, 
            bool DuplicatedUID, bool OnlySpecified, string ContentSpecified
            )
        {
            // 满足筛选条件的所有评论
            var TotalList = new List<Reply>();
            // 抽奖结果评论
            var Result = new List<Reply>();
            // UID集合，用于排除重复UID
            var UIDs = new HashSet<string>();
            var ReplyAPIURL = await Task.Run(() => Helper.GetReplyAPIURL(id));
            var ReplyURL = await Task.Run(() => Helper.GetReplyURL(id));
            // 评论页数
            int PageCount = int.MaxValue;
            for (int i = 1; i <= PageCount; ++i)
            {
                var content = Helper.GetResponse(ReplyAPIURL + i.ToString());
                if (content != string.Empty)
                {
                    var top = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(top["data"].ToString());
                    var page = JsonSerializer.Deserialize<Dictionary<string, int>>(data["page"].ToString());
                    if (i == 1)
                    {
                        if (page["count"] < Count)
                        {
                            return Result;
                        }
                        PageCount = (int)Math.Ceiling(page["count"] / 20.0);
                    }
                    var replies = JsonSerializer.Deserialize<IList>(data["replies"].ToString());
                    foreach (var o_reply in replies)
                    {
                        var reply = JsonSerializer.Deserialize<Dictionary<string, object>>(o_reply.ToString());
                        var rpid = reply["rpid_str"].ToString();
                        var PubTime = new DateTime(1970, 1, 1, 8, 0, 0, DateTimeKind.Utc).AddSeconds(long.Parse(reply["ctime"].ToString()));
                        // 判断开始时间
                        if (!UnlimitedStart && Start >= PubTime &&
                            ((GETStart && Start != PubTime) || !GETStart))
                        {
                            continue;
                        }
                        // 判断结束时间
                        if (!UnlimitedEnd && End <= PubTime &&
                            ((LETEnd && End != PubTime) || !LETEnd))
                        {
                            continue;
                        }
                        var member = JsonSerializer.Deserialize<Dictionary<string, object>>(reply["member"].ToString());
                        var UName = member["uname"].ToString();
                        var Avatar = member["avatar"].ToString();
                        var UID = member["mid"].ToString();
                        // 判断重复UID
                        if (!DuplicatedUID && !UIDs.Add(UID))
                        {
                            continue;
                        }
                        var contents = JsonSerializer.Deserialize<Dictionary<string, object>>(reply["content"].ToString());
                        var Content = contents["message"].ToString();
                        // 判断回复内容
                        if (OnlySpecified && !Content.Contains(ContentSpecified))
                        {
                            continue;
                        }
                        var ReplyToSave = new Reply
                        {
                            Id = rpid,
                            URL = ReplyURL + rpid,
                            LikeCount = int.Parse(reply["like"].ToString()),
                            UID = UID,
                            Content = Content,
                            PubTime = PubTime,
                            UName = member["uname"].ToString(),
                            UserHomeURL = $"https://space.bilibili.com/{UID}",
                            FaceURL = Avatar
                        };
                        TotalList.Add(ReplyToSave);
                    }
                }
                else
                {
                    return Result;
                }
                if (i % 5 == 0)
                {
                    Thread.Sleep(500);
                }
            }
            var TotalListCount = TotalList.Count();
            // 经过条件筛选后的评论数少于预期得奖数，直接返回空列表
            if (TotalListCount < Count)
            {
                return Result;
            }
            Helper.GetRandomResultList(Result, TotalList, Count);
            return Result;
        }
    }
}
