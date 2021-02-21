using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliCLOnline.Models
{
    public class User
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public string UID { get; set; }
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
    }
}
