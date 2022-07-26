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
    public class ReplyController : ControllerBase
    {
        private readonly IReplyResult replyResult;

        public ReplyController(IReplyResult _replyResult)
        {
            replyResult = _replyResult;
        }

        /// <summary>
        /// 获取评论区列表
        /// </summary>
        /// <param name="id">评论承载者标准标识符</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ResultWrapper>> GetLotteryResult(string id)
        {
            // 获取评论列表
            var result = await replyResult.GetList(id);

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
