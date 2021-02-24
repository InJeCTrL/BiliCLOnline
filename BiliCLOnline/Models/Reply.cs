namespace BiliCLOnline.Models
{
    /// <summary>
    /// 评论回复
    /// </summary>
    public class Reply : ItemBase
    {
        /// <summary>
        /// 评论内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 点赞数
        /// </summary>
        public int LikeCount { get; set; }
    }
}
