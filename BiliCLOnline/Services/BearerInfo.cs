using BiliCLOnline.IServices;
using BiliCLOnline.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliCLOnline.Services
{
    public class BearerInfo : IBearerInfo
    {
        public async Task<BearerWrapper> Get(string pattern)
        {
            throw new NotImplementedException();
        }
    }
}
