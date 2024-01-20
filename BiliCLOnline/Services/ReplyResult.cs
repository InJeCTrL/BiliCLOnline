using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiliCLOnline.IServices;
using BiliCLOnline.Models;
using BiliCLOnline.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using static BiliCLOnline.Utils.Constants;

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

        public async Task<Tuple<bool, string, List<Reply>>> GetList(string taskID)
        {
            #region 验证任务ID非空并且存在表中
            if (string.IsNullOrEmpty(taskID) || !helper.guidReplyResults.ContainsKey(taskID))
            {
                logger.LogWarning(message: $"Invalid taskID {taskID}");

                return await Task.FromResult(Tuple.Create(true, "没有此项任务记录", new List<Reply>()));
            }
            #endregion

            #region 验证任务是否执行完成
            if (!helper.guidReplyResults[taskID].Item1)
            {
                logger.LogWarning(message: $"Incomplete task {taskID}");

                return await Task.FromResult(Tuple.Create(false, "仍在获取评论区列表，请稍后", new List<Reply>()));
            }
            #endregion

            #region 任务执行完成, 返回执行结果并移除表中记录
            logger.LogInformation(message: $"Complete task {taskID}");

            helper.guidReplyResults.TryRemove(taskID, out var replyResult);

            return await Task.FromResult(Tuple.Create(
                true, replyResult.Item2, replyResult.Item3));
            #endregion
        }

        private void AddReplyToList(ReplyData replyData, string replyURLPrefix, ConcurrentDictionary<string, Reply> concurrentTotalList)
        {
            if (replyData != null && replyData.replies != null && replyData.replies.Count > 0)
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
                        URL = $"{replyURLPrefix}{rpid}",
                        LikeCount = reply.like,
                        UID = uid,
                        Content = message,
                        PubTime = pubTime,
                        UName = reply.member.uname,
                        UserHomeURL = string.Format(SpaceURLTemplate, uid),
                        FaceURL = reply.member.avatar,
                        Level = reply.member.level_info.current_level
                    };

                    concurrentTotalList.TryAdd(rpid, replyToTotal);
                }
            }
        }

        public async Task<string> InvokeGetListTask(string formalId, string cookie)
        {
            var taskGUID = Guid.NewGuid().ToString();
            helper.guidReplyResults[taskGUID] = Tuple.Create(false, "", new List<Reply>());

            #region 验证formalId有效并且符合语法
            if (!helper.CheckIdSyntax(formalId))
            {
                logger.LogWarning(message: $"Invalid work {formalId}");
                helper.guidReplyResults[taskGUID] = Tuple.Create(true, "作品ID格式错误", new List<Reply>());

                return taskGUID;
            }
            #endregion

            var workBasics = helper.GetBearerBasics(formalId);

            #region 验证评论承载者类型是否合法
            if (workBasics.Item1 == BearerType.Error)
            {
                logger.LogWarning(message: $"Invalid Bearer type id: [{formalId}]");
                helper.guidReplyResults[taskGUID] = Tuple.Create(true, "不支持的评论承载者类型", new List<Reply>());

                return taskGUID;
            }
            #endregion

            var detailAPI = helper.GetBearerDetailAPIURL(workBasics.Item2, workBasics.Item3);

            #region 验证详细信息接口是否合法
            if (string.IsNullOrEmpty(detailAPI))
            {
                logger.LogWarning(message: $"Invalid detailAPI id: [{formalId}] api: [{detailAPI}]");
                helper.guidReplyResults[taskGUID] = Tuple.Create(true, "不合法的详细信息接口", new List<Reply>());

                return taskGUID;
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
                logger.LogWarning(message: $"Invalid work id: [{formalId}]");
                helper.guidReplyResults[taskGUID] = Tuple.Create(true, "无效的作品ID", new List<Reply>());

                return taskGUID;
            }
            #endregion

            // 评论列表
            var concurrentTotalList = new ConcurrentDictionary<string, Reply>();

            // 评论区信息接口前缀
            var replyAPIURLPrefix = helper.GetReplyAPIURL(workBasics.Item2, workBasics.Item3, validDetail.Item2);

            // 评论条目URL前缀
            var replyURLPrefix = helper.GetReplyURLPrefix(workBasics.Item2, workBasics.Item3);
            
            #region 获取评论总条数和首个next
            var firstPage = await webHelper.GetResponse<ReplyData>($"{ replyAPIURLPrefix }", cookie);
            if (firstPage == default)
            {
                logger.LogWarning(message: $"Http request error id: [{formalId}]");
                helper.guidReplyResults[taskGUID] = Tuple.Create(true, "网络请求繁忙，请稍后再试", new List<Reply>());

                return taskGUID;
            }

            var replyCount = firstPage.data.cursor.all_count;
            var firstNext = firstPage.data.cursor.next;
            #endregion

            if (replyCount > MaxReplyLimit)
            {
                logger.LogWarning(message: $"Unsupported work id: [{formalId}]");
                helper.guidReplyResults[taskGUID] =
                    Tuple.Create(true, $"评论数大于{MaxReplyLimit}, 暂不支持抽奖", new List<Reply>());

                return taskGUID;
            }

            #region 首页评论加入列表
            AddReplyToList(firstPage.data, replyURLPrefix, concurrentTotalList);
            #endregion

            // 并发任务列表
            var fillTaskList = new List<Task>();
            _ = Task.Run(async () =>
            {
                var stop = false;
                for (int pageNext = firstNext; pageNext > 0; pageNext -= ReplyPageSize)
                {
                    var next = pageNext;
                    if (stop)
                    {
                        break;
                    }

                    fillTaskList.Add(Task.Run(async () =>
                    {
                        if (stop)
                        {
                            return;
                        }

                        var replyRequestUrl = $"{replyAPIURLPrefix}{next}";
                        var replyAPIReturn = await webHelper.GetResponse<ReplyData>(replyRequestUrl, cookie);
                        if (replyAPIReturn == default)
                        {
                            logger.LogWarning(message: $"Http request error id: [{formalId}], url: [{replyRequestUrl}]");
                            stop = true;

                            return;
                        }

                        AddReplyToList(replyAPIReturn.data, replyURLPrefix, concurrentTotalList);
                    }));
                }

                #region 第一次检查stop 如果有已经stop的 快速响应获取失败的消息
                if (stop)
                {
                    logger.LogWarning(message: $"Http request error id: [{formalId}]");
                    helper.guidReplyResults[taskGUID] = Tuple.Create(true, "网络请求繁忙，请稍后再试", new List<Reply>());
                }
                #endregion

                // 等待所有页取完
                await Task.WhenAll(fillTaskList);

                #region 第二次检查stop 如果tasklist中有stop的 响应获取失败的消息
                if (stop)
                {
                    logger.LogWarning(message: $"Http request error id: [{formalId}]");
                    helper.guidReplyResults[taskGUID] = Tuple.Create(true, "网络请求繁忙，请稍后再试", new List<Reply>());
                }
                #endregion
                #region 获取成功
                else
                {
                    logger.LogInformation(message: $"ReplyResult Succ: {formalId}",
                                        args: new object[] { formalId, replyCount });

                    helper.guidReplyResults[taskGUID] = Tuple.Create(true, "", concurrentTotalList.Values.ToList());
                }
                #endregion
            });

            return taskGUID;
        }
    }
}
