using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliCLOnline.Utils
{
    public class ResultWrapper
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public dynamic Data { get; set; }
        public long Count { get; set; }

    }
}
