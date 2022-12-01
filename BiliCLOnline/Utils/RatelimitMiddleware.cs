using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using static BiliCLOnline.Utils.Constants;

namespace BiliCLOnline.Utils
{
    public class RatelimitMiddleware
    {
        private readonly RequestDelegate next;

        private readonly ILogger<RatelimitMiddleware> logger;

        private readonly IConnectionMultiplexer redisConn;

        private readonly IDatabase db;

        public RatelimitMiddleware(RequestDelegate _next, IConnectionMultiplexer _redisConn, ILogger<RatelimitMiddleware> _logger)
        {
            next = _next;
            redisConn = _redisConn;
            db = redisConn.GetDatabase();
            logger = _logger;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            var reqPath = context.Request.Path.Value ?? "";
            if (reqPath.StartsWith(ReplyRoutePath))
            {
                var ipAddr = context.Connection.RemoteIpAddress.ToString();
                var formalId = reqPath[(reqPath.IndexOf(ReplyRoutePath) + ReplyRoutePath.Length)..];
                var ipId = $"{ipAddr}:{formalId}";

                bool exceedLimit = false;

                #region IP 限制
                if (!db.HashExists(ipAddr, "cnt"))
                {
                    db.HashIncrement(ipAddr, "cnt");
                    db.KeyExpire(ipAddr, DateTime.UtcNow + TimeSpan.FromHours(IPLimitPeriod));
                }
                else
                {
                    var incrd = db.HashIncrement(ipAddr, "cnt");
                    if (incrd >= IPLimitCount)
                    {
                        exceedLimit = true;
                        logger.LogWarning(message: $"Warning: [IPRateLimit] url: [{ipAddr}]");
                    }
                }
                #endregion

                #region IP:formalID 限制
                if (!db.HashExists(ipId, "cnt"))
                {
                    db.HashIncrement(ipId, "cnt");
                    db.KeyExpire(ipId, DateTime.UtcNow + TimeSpan.FromHours(IPIDLimitPeriod));
                }
                else
                {
                    var incrd = db.HashIncrement(ipId, "cnt");
                    if (incrd >= IPIDLimitCount)
                    {
                        exceedLimit = true;
                        logger.LogWarning(message: $"Warning: [IPIDRateLimit] url: [{ipId}]");
                    }
                }
                #endregion

                #region 超过访问限制
                if (exceedLimit)
                {
                    context.Response.ContentType = "application/json; charset=utf-8";

                    await JsonSerializer.SerializeAsync(
                        context.Response.Body,
                        new ResultWrapper
                        {
                            Code = 1,
                            Count = 0,
                            Data = null,
                            Message = "达到访问阈值"
                        },
                        new JsonSerializerOptions(JsonSerializerDefaults.Web));

                    return;
                }
                #endregion
            }

            await next.Invoke(context);
        }
    }
}
