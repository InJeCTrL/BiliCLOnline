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
    public class ReplyResult : IReplyResult
    {
        private readonly Helper helper;

        private readonly WebHelper webHelper;

        private readonly ILogger logger;

        public ReplyResult(Helper _helper, WebHelper _webHelper, ILogger<ReplyResult> _logger)
        {
            helper = _helper;
            webHelper = _webHelper;
            logger = _logger;
        }

        public async Task<Tuple<string, List<Reply>>> GetList(string formalId)
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

            // 评论列表
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
                            var uid = reply.mid;
                            var message = reply.content.message;

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
                                FaceURL = reply.member.avatar,
                                Level = reply.member.level_info.current_level
                            };

                            concurrentTotalList.Add(replyToTotal);
                        }
                    }
                }));
            }

            // 等待所有页取完
            await Task.WhenAll(fillTaskList);

            fillTaskList.Clear();
            fillTaskList = null;

            logger.LogInformation(message: $"ReplyResult Succ: {formalId}",
                                args: new object[] { formalId, replyCount });

            return Tuple.Create("", concurrentTotalList.ToList());
        }
    }
}
