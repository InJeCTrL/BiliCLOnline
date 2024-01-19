using BiliCLOnline.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BiliCLOnline.IServices
{
    public interface IReplyResult
    {
        /// <summary>
        /// 执行获取评论列表的任务
        /// </summary>
        /// <returns>任务ID</returns>
        public Task<string> InvokeGetListTask(string id, string key);

        /// <summary>
        /// 获取任务执行状态
        /// </summary>
        /// <param name="taskID">任务ID</param>
        /// <returns>执行完成, 执行状态提示, 评论列表</returns>
        public Task<Tuple<bool, string, List<Reply>>> GetList(string taskID);
    }
}
