using System.ComponentModel;

namespace BiliCLOnline.Utils
{
    public static class Constants
    {
        public const string ReplyAPITemplate =
            "https://api.bilibili.com/x/v2/reply/main?mode=2&oid={0}&type={1}&ps={2}&next=";

        public const string SpaceURLTemplate =
            "https://space.bilibili.com/{0}";

        public const string VideoURLTemplate =
            "https://www.bilibili.com/video/{0}";

        public const string ArticleURLTemplate =
            "https://www.bilibili.com/read/{0}";

        public const string DynamicURLTemplate =
            "https://t.bilibili.com/{0}";

        public const string LoginQRCodeAPI =
            "https://passport.bilibili.com/x/passport-login/web/qrcode/generate";

        public const string LoginCheckAPITemplate =
            "https://passport.bilibili.com/x/passport-login/web/qrcode/poll?qrcode_key={0}";

        public const string HCaptchaVerifyURL =
            "https://hcaptcha.com/siteverify";

        public const string ReplyRoutePath =
            "/api/Reply/";

        public const int MaxReplyLimit = 40000;

        public const int ReplyPageSize = 30;

        public const int MaxConcurrentFetchLimit = 10;

        public const int MaxTryCount = 5;

        public static class BearerDetailAPITemplate
        {
            public const string AID = "https://api.bilibili.com/x/web-interface/view/detail?aid={0}";
            public const string BVID = "https://api.bilibili.com/x/web-interface/view/detail?bvid={0}";
            public const string CV = "https://api.bilibili.com/x/article/viewinfo?id={0}";
            public const string DID = "https://api.bilibili.com/x/polymer/web-dynamic/v1/detail?id={0}";
        }

        public static class ReplyURLPrefixTemplate
        {
            public const string AID = "https://www.bilibili.com/video/av{0}#reply";
            public const string BVID = "https://www.bilibili.com/video/BV{0}#reply";
            public const string CV = "https://www.bilibili.com/read/cv{0}#reply";
            public const string DID = "https://t.bilibili.com/{0}#reply";
        }
    }
}
