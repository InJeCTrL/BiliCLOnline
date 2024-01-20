using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
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

        public HCaptchaVerifyingMiddleware(RequestDelegate _next, WebHelper _webhelper, ILogger<HCaptchaVerifyingMiddleware> _logger)
        {
            next = _next;
            webHelper = _webhelper;
            secret = Environment.GetEnvironmentVariable("HCaptchaSecret") ?? "";
            logger = _logger;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            #region 获取任务结果路径 & 登录相关 无校验
            if (context.Request.Path.ToString().StartsWith("/api/Confirmation/") ||
                context.Request.Path.ToString().StartsWith("/api/Login/"))
            {
                await next.Invoke(context);
                return;
            }
            #endregion

            #region 校验验证码
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
            #endregion

            #region 校验不通过
            context.Response.ContentType = "application/json; charset=utf-8";

            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                new ResultWrapper
                {
                    Code = 1,
                    Count = 0,
                    Data = null,
                    Message = "验证码校验不通过"
                },
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            #endregion
        }
    }
}
