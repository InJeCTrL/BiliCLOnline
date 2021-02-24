namespace BiliCLOnline.Models
{
    /// <summary>
    /// 视频
    /// </summary>
    public class Video : Media
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
        /// 播放量
        /// </summary>
        public int ViewCount { get; set; }
    }
}
