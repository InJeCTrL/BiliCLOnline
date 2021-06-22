using AspNetCoreRateLimit;
using BiliCLOnline.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliCLOnline
{
    public class CustomIpRateLimitMiddleware : IpRateLimitMiddleware
    {
        public CustomIpRateLimitMiddleware(RequestDelegate next, IProcessingStrategy processingStrategy, IOptions<IpRateLimitOptions> options, IRateLimitCounterStore counterStore, IIpPolicyStore policyStore, IRateLimitConfiguration config, ILogger<IpRateLimitMiddleware> logger) : base(next, processingStrategy, options, counterStore, policyStore, config, logger)
        {
        }

        public override Task ReturnQuotaExceededResponse(HttpContext httpContext, RateLimitRule rule, string retryAfter)
        {
            return HttpResponseJsonExtensions.WriteAsJsonAsync(httpContext.Response, new ResultWrapper
            {
                Code = 429,
                Message = "访问过于频繁，请稍后重试",
                Data = null,
                Count = 0
            });
        }
    }
}
