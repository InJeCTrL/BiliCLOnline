using System;
using System.Threading.Tasks;
using BiliCLOnline.IServices;
using BiliCLOnline.Models;
using BiliCLOnline.Utils;
using Microsoft.Extensions.Logging;

namespace BiliCLOnline.Services
{
    public class BearerInfo : IBearerInfo
    {
        private readonly ILogger logger;

        private readonly Helper helper;
        
        public BearerInfo(Helper _helper, ILogger<BearerInfo> _logger)
        {
            logger = _logger;
            helper = _helper;
        }

        public async Task<BearerWrapper> Get(string pattern)
        {
            var formalId = await helper.GetFormalIdFromPattern(pattern);

            #region 验证formalId有效并且符合语法
            if (string.IsNullOrEmpty(formalId) || !helper.CheckIdSyntax(formalId))
            {
                logger.LogWarning(message: $"Wrong formalId {formalId}, {pattern}");

                return new BearerWrapper
                {
                    Type = BearerType.Error,
                    Bearer = null
                };
            }
            #endregion

            var workBasics = helper.GetBearerBasics(formalId);

            #region 验证评论承载者类型是否合法
            if (workBasics.Item1 == BearerType.Error)
            {
                logger.LogWarning(message: $"Invalid Bearer type {formalId}");

                return new BearerWrapper
                {
                    Type = BearerType.Error,
                    Bearer = null
                };
            }
            #endregion

            var detailAPI = helper.GetBearerDetailAPIURL(workBasics.Item2, workBasics.Item3);

            #region 验证详细信息接口是否合法
            if (string.IsNullOrEmpty(detailAPI))
            {
                logger.LogWarning(message: $"Invalid detailAPI {formalId}, {detailAPI}");

                return new BearerWrapper
                {
                    Type = BearerType.Error,
                    Bearer = null
                };
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
                logger.LogWarning(message: $"Invalid work {pattern}");

                return new BearerWrapper
                {
                    Type = BearerType.Error,
                    Bearer = null
                };
            }
            #endregion

            #region 针对每种评论承载者做对应处理
            switch (workBasics.Item1)
            {
                case BearerType.Video:
                    var videoData = (VideoDetailData)validDetail.Item2;

                    return new BearerWrapper
                    {
                        Type = BearerType.Video,
                        Bearer = new Video
                        {
                            CommentCount = videoData.View.stat.reply,
                            FaceURL = videoData.View.owner.face,
                            Id = formalId,
                            PubTime = helper.TimeTrans(videoData.View.pubdate),
                            ShareCount = videoData.View.stat.share,
                            UID = videoData.View.owner.mid,
                            UName = videoData.View.owner.name,
                            UserHomeURL = string.Format(
                                Constants.SpaceURLTemplate, videoData.View.owner.mid
                                ),
                            URL = string.Format(
                                Constants.VideoURLTemplate, videoData.View.bvid
                                ),
                            CoinCount = videoData.View.stat.coin,
                            CollectCount = videoData.View.stat.favorite,
                            LikeCount = videoData.View.stat.like,
                            Title = videoData.View.title,
                            ViewCount = videoData.View.stat.view
                        }
                    };
                case BearerType.Article:
                    var articleData = (ArticleDetailData)validDetail.Item2;

                    return new BearerWrapper
                    {
                        Type = BearerType.Article,
                        Bearer = new Article
                        {
                            
                            CoinCount = articleData.stats.coin,
                            CollectCount = articleData.stats.favorite,
                            CommentCount = articleData.stats.reply,
                            LikeCount = articleData.stats.like,
                            Id = formalId,
                            ShareCount = articleData.stats.share,
                            Title = articleData.title,
                            ViewCount = articleData.stats.view,
                            UID = articleData.mid,
                            UName = articleData.author_name,
                            UserHomeURL = string.Format(
                                Constants.SpaceURLTemplate, articleData.mid
                                ),
                            URL = string.Format(
                                Constants.ArticleURLTemplate, workBasics.Item3
                            )
                        }
                    };
                case BearerType.Dynamic:
                    var dynamicData = (DynamicDetailData)validDetail.Item2;

                    return new BearerWrapper
                    {
                        Type = BearerType.Dynamic,
                        Bearer = new Dynamic
                        {
                            CommentCount = dynamicData.card.desc.comment,
                            FaceURL = dynamicData.card.desc.user_profile.info.face,
                            PubTime = helper.TimeTrans(dynamicData.card.desc.timestamp),
                            Id = formalId,
                            LikeCount = dynamicData.card.desc.like,
                            ShareCount = dynamicData.card.desc.repost,
                            UID = dynamicData.card.desc.uid,
                            UName = dynamicData.card.desc.user_profile.info.uname,
                            UserHomeURL = string.Format(
                                Constants.SpaceURLTemplate, dynamicData.card.desc.uid
                                ),
                            URL = string.Format(
                                Constants.DynamicURLTemplate, dynamicData.card.desc.dynamic_id
                            )
                        }
                    };
                default:
                    return new BearerWrapper
                    {
                        Type = BearerType.Error,
                        Bearer = null
                    };
            }
            #endregion
        }
    }
}
