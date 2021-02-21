using BiliCLOnline.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliCLOnline.IServices
{
    public interface IBearerInfo
    {
        /// <summary>
        /// 获取评论承载者及其类型
        /// </summary>
        /// <returns></returns>
        public Task<BearerWrapper> Get(string pattern);
    }
}
