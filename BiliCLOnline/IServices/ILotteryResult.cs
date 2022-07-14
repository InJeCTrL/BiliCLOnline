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
        public Task<Tuple<string, List<Reply>>> GetList(
            string id, int count, bool unlimitedStart, bool unlimitedEnd,
            DateTime start, DateTime end, bool GEStart, bool LEEnd,
            bool duplicatedUID, bool onlySpecified, string contentSpecified
            );
    }
}
