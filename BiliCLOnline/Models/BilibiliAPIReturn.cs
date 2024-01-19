using System.Collections.Generic;

namespace BiliCLOnline.Models
{
    public class BilibiliAPIReturn<T> where T : ReturnData
    {
        public int code { get; set; }
        public T data { get; set; }
    }

    /// <summary>
    /// 返回数据
    /// </summary>
    public class ReturnData
    {

    }

    /// <summary>
    /// 详细数据
    /// </summary>
    public class DetailData : ReturnData
    {

    }

    /// <summary>
    /// 动态详细数据
    /// </summary>
    public class DynamicDetailData : DetailData
    {
        public class Item
        {
            public class Basic
            {
                public int comment_type { get; set; }
                public long rid_str { get; set; }
            }
            public class Modules
            {
                public class Module_author
                {
                    public long mid { get; set; }
                    public string name { get; set; }
                    public string face { get; set; }
                    public long pub_ts { get; set; }
                }
                public class Module_stat
                {
                    public class Comment
                    {
                        public int count { get; set; }
                    }
                    public class Forward
                    {
                        public int count { get; set; }
                    }
                    public class Like
                    {
                        public int count { get; set; }
                    }
                    public Comment comment { get; set; }
                    public Forward forward { get; set; }
                    public Like like { get; set; }
                }
                public Module_author module_author { get; set; }
                public Module_stat module_stat { get; set; }
            }
            public Basic basic { get; set; }
            public Modules modules { get; set; }
            public string id_str { get; set; }
        }
        public Item item { get; set; }
    }

    /// <summary>
    /// 视频详细数据
    /// </summary>
    public class VideoDetailData : DetailData
    {
        public class view
        {
            public long pubdate { get; set; }
            public string bvid { get; set; }
            public long aid { get; set; }
            public string title { get; set; }
            public class Owner
            {
                public string face { get; set; }
                public long mid { get; set; }
                public string name { get; set; }
            }
            public Owner owner { get; set; }
            public class Stat
            {
                public int reply { get; set; }
                public int share { get; set; }
                public int coin { get; set; }
                public int favorite { get; set; }
                public int like { get; set; }
                public int view { get; set; }
            }
            public Stat stat { get; set; }
        }
        public view View { get; set; }
    }

    /// <summary>
    /// 专栏详细数据
    /// </summary>
    public class ArticleDetailData : DetailData
    {
        public class Stats
        {
            public int coin { get; set; }
            public int favorite { get; set; }
            public int reply { get; set; }
            public int like { get; set; }
            public int share { get; set; }
            public int view { get; set; }
        }
        public string title { get; set; }
        public long mid { get; set; }
        public string author_name { get; set; }
        public Stats stats { get; set; }
    }

    /// <summary>
    /// 评论数据
    /// </summary>
    public class ReplyData : ReturnData
    {
        public class Reply
        {
            public long mid { get; set; }
            public string rpid_str { get; set; }
            public long ctime { get; set; }
            public class Member
            {
                public string uname { get; set; }
                public string avatar { get; set; }
                public class LevelInfo
                {
                    public int current_level { get; set; }
                }
                public LevelInfo level_info { get; set; }
            }
            public Member member { get; set; }
            public class Content
            {
                public string message { get; set; }
            }
            public Content content { get; set; }
            public int like { get; set; }

        }
        public List<Reply> replies { get; set; }
        public class Cursor
        {
            public int all_count { get; set; }
            public int next { get; set; }
        }
        public Cursor cursor { get; set; }
    }

    /// <summary>
    /// 登录状态检查数据
    /// </summary>
    public class LoginCheckData : ReturnData
    {
        public int code { get; set; }
        public string message { get; set; }
        public string url { get; set; }
    }

    /// <summary>
    /// 登录凭证生成数据
    /// </summary>
    public class LoginGenerateData : ReturnData
    {
        public string qrcode_key { get; set; }
        public string url { get; set; }
    }
}
