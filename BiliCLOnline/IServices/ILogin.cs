using System;
using System.Threading.Tasks;

namespace BiliCLOnline.IServices
{
    public interface ILogin
    {
        /// <summary>
        /// 获取Bilibili登录用二维码
        /// </summary>
        /// <returns><二维码内容, Bilibili登录标识></returns>
        /// <summary>
        public Task<Tuple<string, string>> GetLoginQRCode();
    }
}
