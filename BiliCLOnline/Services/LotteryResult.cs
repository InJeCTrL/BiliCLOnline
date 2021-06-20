using BiliCLOnline.IServices;
using BiliCLOnline.Models;
using BiliCLOnline.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace BiliCLOnline.Services
{
    public class LotteryResult : ILotteryResult
    {
        /// <summary>
        /// 判断待添加的评论是否符合筛选条件
        /// </summary>
        /// <param name="ReplyToSave"></param>
        /// <param name="UnlimitedStart"></param>
        /// <param name="UnlimitedEnd"></param>
        /// <param name="Start"></param>
        /// <param name="End"></param>
        /// <param name="GETStart"></param>
        /// <param name="LETEnd"></param>
        /// <param name="DuplicatedUID"></param>
        /// <param name="selectedReplyUID"></param>
        /// <param name="OnlySpecified"></param>
        /// <param name="ContentSpecified"></param>
        /// <returns></returns>
        private bool Match(
            Reply ReplyToSave, bool UnlimitedStart, bool UnlimitedEnd,
            DateTime Start, DateTime End, bool GETStart, bool LETEnd,
            bool DuplicatedUID, HashSet<string> selectedReplyUID,
            bool OnlySpecified, string ContentSpecified)
        {
            // 判断重复UID
            if (DuplicatedUID || !selectedReplyUID.Contains(ReplyToSave.UID))
            {
                // 判断开始时间
                if (!UnlimitedStart && Start >= ReplyToSave.PubTime &&
                    ((GETStart && Start != ReplyToSave.PubTime) || !GETStart))
                {
                    return false;
                }
                // 判断结束时间
                if (!UnlimitedEnd && End <= ReplyToSave.PubTime &&
                    ((LETEnd && End != ReplyToSave.PubTime) || !LETEnd))
                {
                    return false;
                }
                // 判断回复内容
                if (OnlySpecified && !ReplyToSave.Content.Contains(ContentSpecified))
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 评论json解析为评论对象
        /// </summary>
        /// <param name="ReplyJSON"></param>
        /// <param name="ReplyURL"></param>
        /// <returns></returns>
        private Reply ParseReplyItem(string ReplyJSON, string ReplyURL)
        {
            var reply = JsonSerializer.Deserialize<Dictionary<string, object>>(ReplyJSON);
            var rpid = reply["rpid_str"].ToString();
            var PubTime = new DateTime(1970, 1, 1, 8, 0, 0, DateTimeKind.Utc).AddSeconds(long.Parse(reply["ctime"].ToString()));
            var member = JsonSerializer.Deserialize<Dictionary<string, object>>(reply["member"].ToString());
            var UName = member["uname"].ToString();
            var Avatar = member["avatar"].ToString();
            var UID = member["mid"].ToString();
            var contents = JsonSerializer.Deserialize<Dictionary<string, object>>(reply["content"].ToString());
            var Content = contents["message"].ToString();
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
            return ReplyToSave;
        }
        public IEnumerable<Reply> GetList(
            string id, int Count, bool UnlimitedStart, bool UnlimitedEnd, 
            DateTime Start, DateTime End, bool GETStart, bool LETEnd, 
            bool DuplicatedUID, bool OnlySpecified, string ContentSpecified,
            out string ResultTip
            )
        {
            // 抽奖结果评论
            var Result = new List<Reply>();
            // 评论区信息接口
            var ReplyAPIURL = Helper.GetReplyAPIURL(id);
            if (ReplyAPIURL != string.Empty)
            {
                // 评论条目URL
                var ReplyURL = Helper.GetReplyURL(id);
                if (ReplyURL != string.Empty)
                {
                    // 评论总条数
                    int ReplyCount = int.MaxValue;
                    ResultTip = "";
                    // 评论页数是否有效
                    bool ValidPageCount = false;
                    #region 获取评论总条数
                    var contentFirstPage = WebHelper.GetResponse($"{ ReplyAPIURL }1", "{\"code\":0,");
                    if (contentFirstPage != string.Empty)
                    {
                        var topFirstPage = JsonSerializer.Deserialize<Dictionary<string, object>>(contentFirstPage);
                        var dataFirstPage = JsonSerializer.Deserialize<Dictionary<string, object>>(topFirstPage["data"].ToString());
                        var pageFirstPage = JsonSerializer.Deserialize<Dictionary<string, int>>(dataFirstPage["page"].ToString());
                        ReplyCount = pageFirstPage["count"];
                        ValidPageCount = true;
                    }
                    else
                    {
                        ResultTip = "触发B站风控机制，网络异常，请稍后再试";
                    }
                    #endregion
                    if (ValidPageCount)
                    {
                        // 访问B站接口最大次数
                        var validRequestCount = 200;
                        var From = Enumerable.Range(0, ReplyCount).ToList();
                        var FromLen = ReplyCount;
                        bool err = false;
                        var selectedReplyList = new List<Reply>();
                        var visitedIdx = new HashSet<int>();
                        var selectedReplyUID = new HashSet<string>();
                        while (!err && Count > 0 && FromLen >= Count)
                        {
                            var selectedIdxList = Helper.GetRandomIdxList(From, Count);
                            foreach (var sidx in selectedIdxList)
                            {
                                visitedIdx.Add(sidx);
                            }
                            FromLen -= Count;
                            selectedIdxList.Sort();
                            int nPage = -1;
                            IList replies = new List<object>();
                            foreach (var idx in selectedIdxList)
                            {
                                var tmpPage = idx / 49;
                                var tmpIdxInPage = idx % 49;
                                // 页内可用评论下标列表
                                var FromIdxInPage = new List<int>();
                                for (int i = 0; i < 49; ++i)
                                {
                                    var idxTotal = i + tmpPage * 49;
                                    if (!visitedIdx.Contains(idxTotal) && idxTotal < ReplyCount)
                                    {
                                        FromIdxInPage.Add(i);
                                    }
                                }
                                int FromIdxInPageLen = FromIdxInPage.Count;
                                do
                                {
                                    // 与上一序号不同页
                                    if (tmpPage + 1 != nPage)
                                    {
                                        nPage = tmpPage + 1;
                                        var content = WebHelper.GetResponse($"{ReplyAPIURL}{nPage}", "{\"code\":0,");
                                        --validRequestCount;
                                        if (validRequestCount <= 0)
                                        {
                                            ResultTip = "本次抽奖对B站接口请求次数达到上限，请稍后再试";
                                            err = true;
                                            break;
                                        }
                                        if (content != string.Empty)
                                        {
                                            var top = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                                            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(top["data"].ToString());
                                            var page = JsonSerializer.Deserialize<Dictionary<string, int>>(data["page"].ToString());
                                            if (data["replies"] != null)
                                            {
                                                replies = JsonSerializer.Deserialize<IList>(data["replies"].ToString());
                                            }
                                            else
                                            {
                                                replies = new List<object>();
                                            }
                                        }
                                        else
                                        {
                                            ResultTip = "触发B站风控机制，网络异常，请稍后再试";
                                            err = true;
                                            break;
                                        }
                                    }
                                    var replyCountInPage = replies.Count;
                                    if (tmpIdxInPage < replyCountInPage)
                                    {
                                        var o_reply = replies[tmpIdxInPage];
                                        var ReplyToSave = ParseReplyItem(o_reply.ToString(), ReplyURL);
                                        if (Match(
                                            ReplyToSave, UnlimitedStart, UnlimitedEnd,
                                            Start, End, GETStart, LETEnd,
                                            DuplicatedUID, selectedReplyUID,
                                            OnlySpecified, ContentSpecified
                                            ))
                                        {
                                            selectedReplyUID.Add(ReplyToSave.UID);
                                            selectedReplyList.Add(ReplyToSave);
                                            --Count;
                                            break;
                                        }
                                    }
                                    if (FromLen > 0 && FromIdxInPageLen > 0)
                                    {
                                        tmpIdxInPage = Helper.GetRandomIdxList(FromIdxInPage, 1)[0];
                                        --FromIdxInPageLen;
                                        var idxFrom = From.FindIndex(p => p == (tmpIdxInPage + tmpPage * 49));
                                        if (idxFrom != -1)
                                        {
                                            visitedIdx.Add(tmpIdxInPage + tmpPage * 49);
                                            From[idxFrom] = From[FromLen - 1];
                                            --FromLen;
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                } while (FromLen >= 0 && FromIdxInPageLen >= 0);
                                if (err)
                                {
                                    break;
                                }
                            }
                        }
                        if (FromLen < Count)
                        {
                            ResultTip = "预定中奖评论数大于筛选后的评论数，请重新选择";
                        }
                        else if (!err)
                        {
                            Result = selectedReplyList;
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
