using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliCLOnline.Models
{
    /// <summary>
    /// 评论 与 媒体稿件/动态的共有属性
    /// </summary>
    public class ItemBase : User
    {
        /// <summary>
        /// Id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 发布/发表时间
        /// </summary>
        public DateTime PubTime { get; set; }
        /// <summary>
        /// 跳转到 稿件/专栏/动态/评论 的URL
        /// </summary>
        public string URL { get; set; }
    }
}
