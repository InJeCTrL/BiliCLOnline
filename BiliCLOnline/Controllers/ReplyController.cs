using BiliCLOnline.IServices;
using BiliCLOnline.Utils;
using Microsoft.AspNetCore.Mvc;
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
        /// 执行获取评论区列表任务
        /// </summary>
        /// <param name="id">评论承载者标准标识符</param>
        /// <param name="cookie">Bilibili Cookie</param>
        /// <returns></returns>
        [HttpGet("{id}/{cookie}")]
        public async Task<ActionResult<ResultWrapper>> ExecuteFetchReplyResult(string id, string cookie)
        {
            // 执行获取评论列表任务
            var taskID = await replyResult.InvokeGetListTask(id, cookie);
            
            return new ResultWrapper
            {
                Code = 0,
                Count = 0,
                Data = taskID,
                Message = "已执行查询任务，请稍后"
            };
        }

        /// <summary>
        /// 获取评论区列表
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <returns></returns>
        [HttpGet("/api/Confirmation/{taskID}")]
        public async Task<ActionResult<ResultWrapper>> GetList(string taskID)
        {
            // 获取评论列表
            var result = await replyResult.GetList(taskID);

            var completed = result.Item1;
            var statusTip = result.Item2;
            var replyList = result.Item3;

            if (replyList.Any())
            {
                return new ResultWrapper
                {
                    Code = 0,
                    Count = replyList.Count,
                    Data = replyList,
                    Message = ""
                };
            }
            else if (completed)
            {
                return new ResultWrapper
                {
                    Code = 1,
                    Count = 0,
                    Data = null,
                    Message = statusTip
                };
            }
            else
            {
                return new ResultWrapper
                {
                    Code = 2,
                    Count = 0,
                    Data = null,
                    Message = statusTip
                };
            }
        }
    }
}
