using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliCLOnline.Models
{
    public class Limitation
    {
        /// <summary>
        /// 不限制开始时间
        /// </summary>
        public bool UnlimitedStart { get; set; }
        /// <summary>
        /// 不限制结束时间
        /// </summary>
        public bool UnlimitedEnd { get; set; }
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime Start { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime End { get; set; }
        /// <summary>
        /// 大等于开始时间（包含开始时间点）
        /// </summary>
        public bool GETStart { get; set; }
        /// <summary>
        /// 小等于结束时间（包含结束时间点）
        /// </summary>
        public bool LETEnd { get; set; }
        /// <summary>
        /// 预定中奖评论数
        /// </summary>
        public long Count { get; set; }
        /// <summary>
        /// 同一UID的多条评论也计入抽奖
        /// </summary>
        public bool DuplicatedUID { get; set; }
        /// <summary>
        /// 是否仅统计包含特定文本内容的评论
        /// </summary>
        public bool OnlySpecified { get; set; }
        /// <summary>
        /// 包含的特定文本内容
        /// </summary>
        public HashSet<string> ContentSpecified { get; set; }
    }
}
