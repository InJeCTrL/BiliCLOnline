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
        public class Card
        {
            public class Desc
            {
                public long uid { get; set; }
                public long dynamic_id { get; set; }
                public int comment { get; set; }
                public long timestamp { get; set; }
                public int like { get; set; }
                public int repost { get; set; }
                public int type { get; set; }
                public long rid { get; set; }
                public class UserProfile
                {
                    public class Info
                    {
                        public string face { get; set; }
                        public string uname { get; set; }
                    }
                    public Info info { get; set; }
                }
                public UserProfile user_profile { get; set; }
            }
            public Desc desc { get; set; }
        }
        public Card card { get; set; }
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
        public class Page
        {
            public int acount { get; set; }
        }
        public Page page { get; set; }
    }
}
