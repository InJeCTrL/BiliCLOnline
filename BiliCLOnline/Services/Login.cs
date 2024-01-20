using System;
using System.Threading.Tasks;
using BiliCLOnline.IServices;
using BiliCLOnline.Utils;

namespace BiliCLOnline.Services
{
    public class Login : ILogin
    {
        private readonly WebHelper webHelper;

        public Login(WebHelper _webHelper)
        {
            webHelper = _webHelper;
        }

        public async Task<Tuple<string, string>> GetLoginQRCode()
        {
            return await webHelper.GetBilibiliLoginQRCode();
        }

        public async Task<Tuple<bool, string>> GetLoginResult(string key)
        {
            return await webHelper.VerifyBilibiliLogin(key);
        }
    }
}
