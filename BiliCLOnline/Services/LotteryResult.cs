using BiliCLOnline.IServices;
using BiliCLOnline.Models;
using BiliCLOnline.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliCLOnline.Services
{
    public class LotteryResult : ILotteryResult
    {
        private readonly Helper helper;

        private readonly WebHelper webHelper;

        private readonly ILogger logger;

        public LotteryResult(Helper _helper, WebHelper _webHelper, ILogger<LotteryResult> _logger)
        {
            helper = _helper;
            webHelper = _webHelper;
            logger = _logger;
        }

        public async Task<Tuple<string, List<Reply>>> GetList(
            string formalId, int count, bool unlimitedStart, bool unlimitedEnd,
            DateTime start, DateTime end, bool GEStart, bool LEEnd,
            bool duplicatedUID, bool onlySpecified, string contentSpecified
            )
        {
            #region 验证formalId有效并且符合语法
            if (!helper.CheckIdSyntax(formalId))
            {
                logger.LogWarning(message: "Invalid work",
                                args: new object[] { formalId });

                return Tuple.Create("作品ID格式错误", new List<Reply>());
            }
            #endregion

            var workBasics = helper.GetBearerBasics(formalId);

            #region 验证评论承载者类型是否合法
            if (workBasics.Item1 == BearerType.Error)
            {
                logger.LogWarning(message: "Invalid Bearer type",
                                args: new object[] { formalId });

                return Tuple.Create("不支持的评论承载者类型", new List<Reply>());
            }
            #endregion

            var detailAPI = helper.GetBearerDetailAPIURL(workBasics.Item2, workBasics.Item3);

            #region 验证详细信息接口是否合法
            if (string.IsNullOrEmpty(detailAPI))
            {
                logger.LogWarning(message: "Invalid detailAPI",
                                args: new object[] { formalId, detailAPI });

                return Tuple.Create("不合法的详细信息接口", new List<Reply>());
            }
            #endregion

            Tuple<bool, DetailData> validDetail = workBasics.Item1 switch
            {
                BearerType.Dynamic => await helper.IsValidWork<DynamicDetailData>(detailAPI),
                BearerType.Article => await helper.IsValidWork<ArticleDetailData>(detailAPI),
                BearerType.Video => await helper.IsValidWork<VideoDetailData>(detailAPI),
                BearerType.Error => Tuple.Create(false, default(DetailData)),
                _ => throw new NotImplementedException()
            };

            #region 验证是否有效作品
            if (!validDetail.Item1)
            {
                logger.LogWarning(message: "Invalid work",
                                args: new object[] { formalId });

                Tuple.Create("无效的作品ID", Enumerable.Empty<Reply>());
            }
            #endregion

            #region 标准化起止时间
            if (!unlimitedStart && GEStart)
            {
                start = start.AddSeconds(-1);
            }

            if (!unlimitedEnd && LEEnd)
            {
                end = end.AddSeconds(1);
            }
            #endregion

            // 抽奖结果评论
            var result = new List<Reply>();

            // 加入到预抽选列表的UID列表
            var existUID = new ConcurrentDictionary<long, byte>();

            // 预抽选列表
            var concurrentTotalList = new ConcurrentBag<Reply>();

            // 评论区信息接口前缀
            var replyAPIURLPrefix = helper.GetReplyAPIURL(workBasics.Item2, workBasics.Item3, validDetail.Item2);

            // 评论条目URL前缀
            var replyURLPrefix = helper.GetReplyURLPrefix(workBasics.Item2, workBasics.Item3);
            
            #region 获取评论总条数
            var firstPage = await webHelper.GetResponse<ReplyData>($"{ replyAPIURLPrefix }1");

            var replyCount = firstPage.data.page.count;
            #endregion

            if (replyCount > 40000)
            {
                logger.LogWarning(message: "Unsupported work",
                                args: new object[] { formalId, replyCount });

                return Tuple.Create("评论数大于4万, 暂不支持抽奖", new List<Reply>());
            }

            var fillTaskList = new List<Task>();

            // 评论页数
            int pageCnt = (int)Math.Ceiling(replyCount / 49.0);
            for (int i = 1; i <= pageCnt; ++i)
            {
                var idxPage = i;
                fillTaskList.Add(Task.Run(async () =>
                {
                    var replyAPIReturn = await webHelper.GetResponse<ReplyData>($"{ replyAPIURLPrefix }{ idxPage }");

                    var replyData = replyAPIReturn.data;

                    if (replyData.replies != null && replyData.replies.Count > 0)
                    {
                        foreach (var reply in replyData.replies)
                        {
                            var rpid = reply.rpid_str;

                            var pubTime = helper.TimeTrans(reply.ctime);

                            #region 判断开始时间
                            if (!unlimitedStart && start >= pubTime)
                            {
                                continue;
                            }
                            #endregion

                            #region 判断结束时间
                            if (!unlimitedEnd && end <= pubTime)
                            {
                                continue;
                            }
                            #endregion

                            var uid = reply.mid;

                            var message = reply.content.message;

                            #region 判断重复UID
                            if (!duplicatedUID && !existUID.TryAdd(uid, 0))
                            {
                                continue;
                            }
                            #endregion

                            #region 判断回复内容
                            if (onlySpecified && !message.Contains(contentSpecified))
                            {
                                continue;
                            }
                            #endregion

                            var replyToTotal = new Reply
                            {
                                Id = rpid,
                                URL = $"{ replyURLPrefix }{ rpid }",
                                LikeCount = reply.like,
                                UID = uid,
                                Content = message,
                                PubTime = pubTime,
                                UName = reply.member.uname,
                                UserHomeURL = string.Format(Constants.SpaceURLTemplate, uid),
                                FaceURL = reply.member.avatar
                            };

                            concurrentTotalList.Add(replyToTotal);
                        }
                    }
                }));
            }

            // 等待所有页取完
            await Task.WhenAll(fillTaskList);

            existUID.Clear();
            existUID = null;

            fillTaskList.Clear();
            fillTaskList = null;

            var totalListCount = concurrentTotalList.Count;

            // 经过条件筛选后的评论数小于预期得奖数
            if (totalListCount < count)
            {
                logger.LogWarning(message: "Total count does not meet",
                                args: new object[] { formalId, count, totalListCount });

                return Tuple.Create("预定中奖评论数大于筛选后的评论数，请重新选择", new List<Reply>());
            }

            var idxList = helper.GetRandomIdxList(totalListCount, count);
            var totalList = concurrentTotalList.ToList();

            concurrentTotalList.Clear();
            concurrentTotalList = null;

            foreach (var idx in idxList)
            {
                result.Add(totalList[idx]);
            }

            totalList.Clear();
            idxList.Clear();

            return Tuple.Create("", result);
        }
    }
}
