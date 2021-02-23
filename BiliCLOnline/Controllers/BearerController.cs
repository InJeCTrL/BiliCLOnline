using BiliCLOnline.IServices;
using BiliCLOnline.Models;
using BiliCLOnline.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliCLOnline.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BearerController : ControllerBase
    {
        private readonly IBearerInfo _bearerInfo;
        public BearerController(IBearerInfo bearerInfo)
        {
            _bearerInfo = bearerInfo;
        }
        /// <summary>
        /// 获取评论承载者（媒体稿件/动态）的信息
        /// </summary>
        /// <param name="pattern">用于搜索评论承载者的特征串</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<ResultWrapper>> GetBearer(string pattern)
        {
            var wrapper = await _bearerInfo.Get(pattern);
            if (wrapper.Type == BearerType.Error)
            {
                return new ResultWrapper
                {
                    Code = 1,
                    Count = 0,
                    Data = null,
                    Message = "ID或URL无效"
                };
            }
            else
            {
                return new ResultWrapper
                {
                    Code = 0,
                    Count = 1,
                    Data = wrapper,
                    Message = ""
                };
            }
        }
    }
}
