namespace BiliCLOnline.Models
{
    public class User
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public long UID { get; set; }
        /// <summary>
        /// 用户名
        /// </summary>
        public string UName { get; set; }
        /// <summary>
        /// 用户头像图片URL
        /// </summary>
        public string FaceURL { get; set; }
        /// <summary>
        /// 用户主页URL
        /// </summary>
        public string UserHomeURL { get; set; }
        /// <summary>
        /// 用户等级
        /// </summary>
        public int Level { get; set; }
    }
}
