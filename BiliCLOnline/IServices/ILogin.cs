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
        public Task<Tuple<string, string>> GetLoginQRCode();

        /// <summary>
        /// 获取登录状态
        /// </summary>
        /// <param name="key">Bilibili登录标识</param>
        /// <returns><是否通过验证, Cookie></returns>
        public Task<Tuple<bool, string>> GetLoginResult(string key);
    }
}
