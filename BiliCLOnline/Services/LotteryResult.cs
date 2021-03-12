using BiliCLOnline.IServices;
using BiliCLOnline.Models;
using BiliCLOnline.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;

namespace BiliCLOnline.Services
{
    public class LotteryResult : ILotteryResult
    {
        public IEnumerable<Reply> GetList(
            string id, int Count, bool UnlimitedStart, bool UnlimitedEnd, 
            DateTime Start, DateTime End, bool GETStart, bool LETEnd, 
            bool DuplicatedUID, bool OnlySpecified, string ContentSpecified,
            out string ResultTip
            )
        {
            // 满足筛选条件的所有评论
            var TotalList = new List<Reply>();
            // 抽奖结果评论
            var Result = new List<Reply>();
            // UID集合，用于排除重复UID
            var UIDs = new HashSet<string>();
            // 评论区信息接口
            var ReplyAPIURL = Helper.GetReplyAPIURL(id);
            if (ReplyAPIURL != string.Empty)
            {
                // 评论条目URL
                var ReplyURL = Helper.GetReplyURL(id);
                if (ReplyURL != string.Empty)
                {
                    // 评论页数
                    int PageCount = int.MaxValue;
                    ResultTip = "";
                    for (int i = 1; i <= PageCount; ++i)
                    {
                        var content = WebHelper.GetResponse(ReplyAPIURL + i.ToString(), "{\"code\":0,");
                        if (content != string.Empty)
                        {
                            var top = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(top["data"].ToString());
                            var page = JsonSerializer.Deserialize<Dictionary<string, int>>(data["page"].ToString());
                            // 第一页需要设置评论总页数
                            if (i == 1)
                            {
                                if (page["count"] < Count)
                                {
                                    break;
                                }
                                PageCount = (int)Math.Ceiling(page["count"] / 49.0);
                                if (PageCount > 300)
                                {
                                    ResultTip = "抽奖目标的评论页数过多，拒绝抽奖";
                                    break;
                                }
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
                            // 最后一页
                            if (i == PageCount)
                            {
                                var TotalListCount = TotalList.Count;
                                // 经过条件筛选后的评论数大等于预期得奖数
                                if (TotalListCount >= Count)
                                {
                                    Helper.GetRandomResultList(Result, TotalList, Count);
                                }
                                else
                                {
                                    ResultTip = "预定中奖评论数大于筛选后的评论数，请重新选择";
                                }
                            }
                        }
                        else
                        {
                            ResultTip = "触发B站风控机制，网络异常，请稍后再试";
                            break;
                        }
                    }
                }
                else
                {
                    ResultTip = "暂不支持对此类型作品进行抽奖";
                }
            }
            else
            {
                ResultTip = "获取评论区信息接口失败，请稍后重试";
            }
            return Result;
        }
    }
}
