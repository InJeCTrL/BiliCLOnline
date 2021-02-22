using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliCLOnline.Models
{
    /// <summary>
    /// 媒体稿件（视频/专栏） 的共有属性
    /// </summary>
    public class Media : Bearer
    {
        /// <summary>
        /// 媒体稿件标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 收藏数
        /// </summary>
        public int CollectCount { get; set; }
    }
}
