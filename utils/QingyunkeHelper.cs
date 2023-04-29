using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.boot;
using RS.Tools.Common.Utils;
using RS.Tools.Network.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace RS.Snail.JJJ.utils
{
    internal class QingyunkeHelper
    {
        public const string TAG = "QingyunkeHelper";
        private static string URL(string key) => $"http://api.qingyunke.com/api.php?key=free&appid=0&msg={key}";

        private static Random _rand = new Random();

        private static object _lock = new object();


        public static async Task<string> GetResponse(string key)
        {
            try
            {
                return await Tools.Network.Client.HTTPGet.GetAsync(URL(key));
            }
            catch (Exception ex)
            {
                Logger.Instance.Write(ex, "Qingyunke");
                return "";
            }
        }
        /// <summary>
        /// 从青云客获得回应
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key"></param>
        /// <param name="robotWxid"></param>
        /// <param name="sender"></param>
        /// <param name="wxid"></param>
        /// <param name="force"></param>
        public async static Task<string?> GetResponseAsync(Context context, string key, string wxid, bool force = false)
        {

            try
            {
                if (!force)
                {
                    var cdKey = $"qingyunke_{wxid}";
                    var cd = context.CdM.GetCache(cdKey);
                    var now = TimeHelper.ToTimeStamp();
                    if (cd + 600 < now) return null;
                    context.CdM.SetCache(cdKey, now);
                }

                string data;
                data = await Tools.Network.Client.HTTPGet.GetAsync(URL(key));
                if (string.IsNullOrEmpty(data)) return null;
                data = JSONHelper.ParseString(JObject.Parse(data)["content"]);
                if (string.IsNullOrEmpty(data)) return null;
                return FixConversationContent(data);
            }
            catch (Exception ex)
            {
                Logger.Instance.Write(ex, TAG);
                return null;
            }
        }





        private static string FixConversationContent(string raw)
        {
            //是否开启骂人模式

            //foreach (var item in _Special_Content)
            //{
            //    if (raw.Contains(item)) return RandomReply();
            //}

            raw = raw.Replace("{br}", "\n");
            raw = raw.Replace("菲菲", "唧唧叽");
            raw = raw.Replace("关注新浪微博leisure磊", "要去问本会会长");
            raw = raw.Replace("梅州行论坛", $"唧唧宇宙总裁办");
            raw = raw.Replace("www.ai-cn.net", $"https://v.douyin.com/YbswS2b/");
            raw = raw.Replace("梅州行", $"叽叽宇宙");

            if (raw.Contains("face:"))
            {
                var emoji = "";
                for (int i = 0; i <= 104; i++)
                {
                    emoji = "{" + $"face:{i}" + "}";
                    if (raw.Contains(emoji))
                    {
                        raw = raw.Replace(emoji, ConvertEmoji(i));
                    }
                }
            }
            return raw;
        }
        private static List<string> _Special_Content = new List<string>()
        {
            "没有亲人",
            "对不起",
            "爸爸",
            "口交",
            "好的",
            "是的",
            "当然",
            "对的",
            "抱歉",
            "全部吃掉",
        };
        private static List<string> _Reply_Dirty = new List<string>
        {
            "捏嘛的",
            "我草你妈",
            "小瘪犊子",
            "滚吧",
            "你完了",
            "闭嘴吧",
            "去你的吧",
            "都不稀得理你",
            "去你吗的",
            "rnm",
            "cnm",
            "wcnm",
            "草泥马",
            "日你仙人板板儿",
            "妈卖批",
            "大傻逼",
        };
        private static string RandomReply() => _Reply_Dirty[_rand.Next(0, _Reply_Dirty.Count)];
        private static string ConvertEmoji(int index) => index switch
        {
            0 => "[微笑]",
            1 => "[撇嘴]",
            2 => "[色]",
            3 => "[发呆]",
            4 => "[得意]",
            5 => "[流泪]",
            6 => "[害羞]",
            7 => "[闭嘴]",
            8 => "[睡]",
            9 => "[大哭]",
            10 => "[尴尬]",
            11 => "[发怒]",
            12 => "[调皮]",
            13 => "[呲牙]",
            14 => "[惊讶]",
            15 => "[难过]",
            16 => "[酷]",
            17 => "[冷汗]",
            18 => "[抓狂]",
            19 => "[吐]",
            20 => "[偷笑]",
            21 => "[可爱]",
            22 => "[白眼]",
            23 => "[傲慢]",
            24 => "[饥饿]",
            25 => "[困]",
            26 => "[惊恐]",
            27 => "[流汗]",
            28 => "[憨笑]",
            29 => "[大兵]",
            30 => "[奋斗]",
            31 => "[咒骂]",
            32 => "[疑问]",
            33 => "[嘘]",
            34 => "[晕]",
            35 => "[折磨]",
            36 => "[衰]",
            37 => "[骷髅]",
            38 => "[敲打]",
            39 => "[再见]",
            40 => "[擦汗]",
            41 => "[抠鼻]",
            42 => "[鼓掌]",
            43 => "[糗大了]",
            44 => "[坏笑]",
            45 => "[左哼哼]",
            46 => "[右哼哼]",
            47 => "[哈欠]",
            48 => "[鄙视]",
            49 => "[委屈]",
            50 => "[快哭了]",
            51 => "[阴险]",
            52 => "[亲亲]",
            53 => "[吓]",
            54 => "[可怜]",
            55 => "[菜刀]",
            56 => "[西瓜]",
            57 => "[啤酒]",
            58 => "[篮球]",
            59 => "[乒乓]",
            60 => "[咖啡]",
            61 => "[饭]",
            62 => "[猪头]",
            63 => "[玫瑰]",
            64 => "[凋谢]",
            65 => "[示爱]",
            66 => "[爱心]",
            67 => "[心碎]",
            68 => "[蛋糕]",
            69 => "[闪电]",
            70 => "[炸弹]",
            71 => "[刀]",
            72 => "[足球]",
            73 => "[瓢虫]",
            74 => "[便便]",
            75 => "[月亮]",
            76 => "[太阳]",
            77 => "[礼物]",
            78 => "[拥抱]",
            79 => "[强]",
            80 => "[弱]",
            81 => "[握手]",
            82 => "[胜利]",
            83 => "[抱拳]",
            84 => "[勾引]",
            85 => "[拳头]",
            86 => "[差劲]",
            87 => "[爱你]",
            88 => "[NO]",
            89 => "[OK]",
            90 => "[爱情]",
            91 => "[飞吻]",
            92 => "[跳跳]",
            93 => "[发抖]",
            94 => "[怄火]",
            95 => "[转圈]",
            96 => "[磕头]",
            97 => "[回头]",
            98 => "[跳绳]",
            99 => "[挥手]",
            100 => "[激动]",
            101 => "[街舞]",
            102 => "[献吻]",
            103 => "[左太极]",
            104 => "[右太极]",
            _ => "",
        };
    }
}
