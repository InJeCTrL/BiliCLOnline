using BiliCLOnline.IServices;
using BiliCLOnline.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BiliCLOnline.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LotteryController : ControllerBase
    {
        private readonly ILotteryResult lotteryResult;

        public LotteryController(ILotteryResult _lotteryResult)
        {
            lotteryResult = _lotteryResult;
        }

        /// <summary>
        /// 获取评论区抽奖结果
        /// </summary>
        /// <param name="id">评论承载者标准标识符</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ResultWrapper>> GetLotteryResult(
            string id, int count, bool unlimitedStart, bool unlimitedEnd,
            DateTime start, DateTime end, bool GEStart, bool LEEnd,
            bool duplicatedUID, bool onlySpecified, string contentSpecified
            )
        {
            if (string.IsNullOrEmpty(contentSpecified))
            {
                contentSpecified = "";
            }

            if (count == 0)
            {
                return new ResultWrapper
                {
                    Code = 1,
                    Count = 0,
                    Data = null,
                    Message = "期望中奖评论数需大于0"
                };
            }
            else if (count > 50)
            {
                return new ResultWrapper
                {
                    Code = 1,
                    Count = 0,
                    Data = null,
                    Message = "期望中奖评论数需小等于50"
                };
            }

            // 获取抽奖结果
            var result = await lotteryResult.GetList(
                id, count, unlimitedStart, unlimitedEnd,
                start, end, GEStart, LEEnd, duplicatedUID,
                onlySpecified, contentSpecified
                );

            var replyList = result.Item2;

            if (!replyList.Any())
            {
                return new ResultWrapper
                {
                    Code = 1,
                    Count = 0,
                    Data = null,
                    Message = result.Item1
                };
            }
            else
            {
                return new ResultWrapper
                {
                    Code = 0,
                    Count = replyList.Count,
                    Data = replyList,
                    Message = ""
                };
            }
        }
    }
}
