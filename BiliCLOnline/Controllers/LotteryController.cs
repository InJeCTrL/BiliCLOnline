using BiliCLOnline.IServices;
using BiliCLOnline.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliCLOnline.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LotteryController : ControllerBase
    {
        private readonly ILotteryResult _lotteryResult;
        public LotteryController(ILotteryResult lotteryResult)
        {
            _lotteryResult = lotteryResult;
        }
        /// <summary>
        /// 获取评论区抽奖结果
        /// </summary>
        /// <param name="id">评论承载者标准标识符</param>
        /// <returns></returns>
        [HttpPost("{id}")]
        public async Task<ActionResult<ResultWrapper>> GetLotteryResult(
            string id, int Count, bool UnlimitedStart, bool UnlimitedEnd,
            DateTime Start, DateTime End, bool GETStart, bool LETEnd,
            bool DuplicatedUID, bool OnlySpecified, string ContentSpecified
            )
        {
            if (Count == 0)
            {
                return new ResultWrapper
                {
                    Code = 1,
                    Count = 0,
                    Data = null,
                    Message = "期望中奖评论数需大于0"
                };
            }
            // 判断评论承载者标准标识符是否合法
            var IsFormalId = await Task.Run(() => Helper.CheckIdHead(id));
            if (!IsFormalId)
            {
                return new ResultWrapper
                {
                    Code = 1,
                    Count = 0,
                    Data = null,
                    Message = "非法的评论承载者标识符"
                };
            }
            // 检查是否是有效标识符
            var IsValidId = await Task.Run(() => Helper.IsValidId(id));
            if (!IsValidId)
            {
                return new ResultWrapper
                {
                    Code = 1,
                    Count = 0,
                    Data = null,
                    Message = "无效稿件/动态"
                };
            }
            // 获取抽奖结果
            var ReplyList = await _lotteryResult.GetList(
                                    id, Count, UnlimitedStart, UnlimitedEnd,
                                    Start, End, GETStart, LETEnd, DuplicatedUID,
                                    OnlySpecified, ContentSpecified
                                    );
            if (!ReplyList.Any())
            {
                return new ResultWrapper
                {
                    Code = 1,
                    Count = 0,
                    Data = null,
                    Message = "预定中奖评论数大于筛选后的评论数，请重新选择"
                };
            }
            else
            {
                return new ResultWrapper
                {
                    Code = 0,
                    Count = ReplyList.Count(),
                    Data = ReplyList,
                    Message = ""
                };
            }
        }
    }
}
