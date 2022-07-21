using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;

namespace BiliCLOnline.Utils
{
    public class HCaptchaVerifyingMiddleware
    {
        private readonly RequestDelegate next;

        private readonly ILogger<HCaptchaVerifyingMiddleware> logger;

        private readonly WebHelper webHelper;

        private readonly string secret;

        public HCaptchaVerifyingMiddleware(RequestDelegate _next, WebHelper _webhelper, IConfiguration config, ILogger<HCaptchaVerifyingMiddleware> _logger)
        {
            next = _next;
            webHelper = _webhelper;
            secret = config.GetValue<string>("HCaptchaSecret");
            logger = _logger;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            var hasHCT = context.Request.Headers.TryGetValue("h-captcha-response", out var hCTResponse);

            if (hasHCT)
            {
                var hCaptchaToken = hCTResponse.ToString();

                if (await webHelper.VerifyCaptcha(hCaptchaToken, secret))
                {
                    await next.Invoke(context);
                    return;
                }
            }

            #region 校验不通过
            context.Response.ContentType = "application/json; charset=utf-8";

            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                new ResultWrapper
                {
                    Code = 1,
                    Count = 0,
                    Data = null,
                    Message = "Captcha validation not passed"
                },
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            #endregion
        }
    }
}
