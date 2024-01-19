using BiliCLOnline.IServices;
using BiliCLOnline.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BiliCLOnline.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ILogin login;

        public LoginController(ILogin _login)
        {
            login = _login;
        }

        /// <summary>
        /// 获取提取前登录二维码内容
        /// </summary>
        /// <returns></returns>
        [HttpGet("qrcode")]
        public async Task<ActionResult<ResultWrapper>> GetQRCode()
        {
            // 获取Bilibili登录用二维码
            var qrContent = await login.GetLoginQRCode();
            if (string.IsNullOrEmpty(qrContent.Item1))
            {
                return new ResultWrapper
                {
                    Code = -1,
                    Count = 0,
                    Data = "",
                    Message = "登录二维码获取失败"
                };
            }

            return new ResultWrapper
            {
                Code = 0,
                Count = 0,
                Data = qrContent.Item1,
                Message = qrContent.Item2
            };
        }
    }
}
