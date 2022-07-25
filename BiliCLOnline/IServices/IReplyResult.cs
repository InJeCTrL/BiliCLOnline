using BiliCLOnline.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BiliCLOnline.IServices
{
    public interface IReplyResult
    {
        /// <summary>
        /// 获取评论列表
        /// </summary>
        /// <returns></returns>
        public Task<Tuple<string, List<Reply>>> GetList(string id);
    }
}
