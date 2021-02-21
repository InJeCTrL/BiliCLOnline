using BiliCLOnline.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliCLOnline.Models
{
    public class BearerWrapper
    {
        /// <summary>
        /// 评论承载者类型
        /// </summary>
        public BearerType Type { get; set; }
        /// <summary>
        /// 评论承载者对象
        /// </summary>
        public dynamic Bearer { get; set; }
    }
}
