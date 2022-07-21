namespace BiliCLOnline.Utils
{
    public static class Constants
    {
        public const string ScrapingAntAPIPrefix =
            "https://api.scrapingant.com/v1/general?return_text=true&url=";

        public const string ReplyAPITemplate =
            "http://api.bilibili.com/x/v2/reply?oid={0}&type={1}&sort=1&ps=49&pn=";

        public const string SpaceURLTemplate =
            "https://space.bilibili.com/{0}";

        public const string VideoURLTemplate =
            "https://www.bilibili.com/video/{0}";

        public const string ArticleURLTemplate =
            "https://www.bilibili.com/read/{0}";

        public const string DynamicURLTemplate =
            "https://t.bilibili.com/{0}";

        public const string HCaptchaVerifyURL =
            "https://hcaptcha.com/siteverify";

        public static class BearerDetailAPITemplate
        {
            public const string AID = "http://api.bilibili.com/x/web-interface/view/detail?aid={0}";
            public const string BVID = "http://api.bilibili.com/x/web-interface/view/detail?bvid={0}";
            public const string CV = "http://api.bilibili.com/x/article/viewinfo?id={0}";
            public const string DID = "http://api.vc.bilibili.com/dynamic_svr/v1/dynamic_svr/get_dynamic_detail?dynamic_id={0}";
        }

        public static class ReplyURLPrefixTemplate
        {
            public const string AID = "http://www.bilibili.com/video/av{0}#reply";
            public const string BVID = "http://www.bilibili.com/video/BV{0}#reply";
            public const string CV = "http://www.bilibili.com/read/cv{0}#reply";
            public const string DID = "http://t.bilibili.com/{0}#reply";
        }
    }
}
