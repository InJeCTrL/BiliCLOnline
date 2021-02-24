namespace BiliCLOnline.Models
{
    /// <summary>
    /// 动态
    /// </summary>
    public class Dynamic : Bearer
    {
        /// <summary>
        /// 点赞数
        /// </summary>
        public int LikeCount { get; set; }
    }
}
