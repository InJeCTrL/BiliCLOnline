using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliCLOnline.Models
{
    /// <summary>
    /// 专栏
    /// </summary>
    public class Article : Media
    {
        /// <summary>
        /// 点赞数
        /// </summary>
        public int LikeCount { get; set; }
        /// <summary>
        /// 投币量
        /// </summary>
        public int CoinCount { get; set; }
        /// <summary>
        /// 阅读量
        /// </summary>
        public int ViewCount { get; set; }
    }
}
