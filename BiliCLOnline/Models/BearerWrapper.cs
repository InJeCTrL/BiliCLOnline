using BiliCLOnline.Utils;

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
