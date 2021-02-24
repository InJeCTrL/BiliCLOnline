using BiliCLOnline.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BiliCLOnline.IServices
{
    public interface ILotteryResult
    {
        /// <summary>
        /// 获取抽奖结果评论列表
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<Reply>> GetList(
            string id, int Count, bool UnlimitedStart, bool UnlimitedEnd,
            DateTime Start, DateTime End, bool GETStart, bool LETEnd,
            bool DuplicatedUID, bool OnlySpecified, string ContentSpecified
            );
    }
}
