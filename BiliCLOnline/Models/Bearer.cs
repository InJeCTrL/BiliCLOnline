using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliCLOnline.Models
{
    /// <summary>
    /// 媒体稿件与动态 的共有属性
    /// </summary>
    public class Bearer : ItemBase
    {
        /// <summary>
        /// 评论条数（不包含楼中楼）
        /// </summary>
        public int CommentCount { get; set; }
        /// <summary>
        /// 转发数
        /// </summary>
        public int ShareCount { get; set; }
    }
}
