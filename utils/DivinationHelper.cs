using Newtonsoft.Json.Linq;
using RS.Tools.Common.Utils;
using RS.Tools.Network.Sockets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.utils
{
    /// <summary>
    /// 算卦
    /// </summary>
    internal class DivinationHelper
    {
        private static dynamic _guaData;
        private static bool _inited = false;

        public static void Init()
        {
            if (_inited) return;
            _inited = true;
            _guaData = IOHelper.GetCSV(Tools.Common.Enums.CSVType.RobotData, "zhouyi64") ?? new JObject();
        }

        // 乾一兑二离三震四巽(xun4)五坎六艮(geng4)七坤八
        private static Dictionary<int, string> _eights = new()
        {
            { 0, "乾" },
            { 1, "兑" },
            { 2, "离" },
            { 3, "震" },
            { 4, "巽" },
            { 5, "坎" },
            { 6, "艮" },
            { 7, "坤" },
        };

        // 子一，丑二，寅三，卯四，辰五，巳六，午七，未八，申九，西十，戌十一，亥十二
        private static Dictionary<int, string> _twelveHour = new()
        {
            { 1, "子" },
            { 2, "丑" },
            { 3, "寅" },
            { 4, "卯" },
            { 5, "辰" },
            { 6, "巳" },
            { 7, "午" },
            { 8, "未" },
            { 9, "申" },
            { 10, "西" },
            { 11, "戌" },
            { 12, "亥" },
        };
        /// <summary>
        /// 根据当前时间取时辰
        /// </summary>
        /// <returns></returns>
        private static string GetHourName()
        {
            var now = DateTime.Now.Hour;
            return now switch
            {
                1 or 2 => _twelveHour[2],
                3 or 4 => _twelveHour[3],
                5 or 6 => _twelveHour[4],
                7 or 8 => _twelveHour[5],
                9 or 10 => _twelveHour[6],
                11 or 12 => _twelveHour[7],
                13 or 14 => _twelveHour[8],
                15 or 16 => _twelveHour[9],
                17 or 18 => _twelveHour[10],
                19 or 20 => _twelveHour[11],
                21 or 22 => _twelveHour[12],
                _ => _twelveHour[1],
            };
        }

        private static int GetHourIndex(int hour = -1)
        {
            var now = hour > 0 ? hour : DateTime.Now.Hour;
            return now switch
            {
                1 or 2 => 2,
                3 or 4 => 3,
                5 or 6 => 4,
                7 or 8 => 5,
                9 or 10 => 6,
                11 or 12 => 7,
                13 or 14 => 8,
                15 or 16 => 9,
                17 or 18 => 10,
                19 or 20 => 11,
                21 or 22 => 12,
                _ => 1,
            };
        }

        private static List<string> _divinations = new List<string>()
            {
                "大吉", // 1+1
                "小吉", // 1+0
                "平相", // 0+0 / 1+-1
                "小凶", // 0+-1
                "大凶", // -1+-1
            };

        private static string DivinationFinal(int val1, int val2) => (val1 + val2) switch
        {
            2 => "大吉",
            1 => "小吉",
            0 => "平相",
            -1 => "大吉",
            -2 => "小凶",
            _ => "大凶",
        };

        public static string Test(int num, int hour)
        {
            var digits = GetDigitList(num);
            int waiGua = 0;
            int neiGua = 0;
            var waiDigit = digits.Count / 2;
            for (int i = 0; i < digits.Count; i++)
            {
                if (i < waiDigit) waiGua += digits[i];
                else neiGua += digits[i];
            }
            waiGua--;
            neiGua--;

            waiGua %= 8;
            neiGua %= 8;

            var symbol = new HexSymbol(waiGua, neiGua);
            var symbolOriName = symbol.Name;

            // 变卦
            var changeIndex = (waiGua + neiGua + GetHourIndex(hour)) % 6;
            symbol.Change(changeIndex);

            var symbolChangedName = symbol.Name;

            return $"数字:{num}, 时辰:{hour}\n本卦:{symbolOriName}, 变卦:{symbolChangedName}";
        }

        public static string GetDivination(int number)
        {
            if (number < 100) return "请输入一个三位数及以上的整数";

            Init();

            // 吉 平 凶
            // +1 0 -1


            var digits = GetDigitList(number);
            int waiGua = 0;
            int neiGua = 0;
            var waiDigit = digits.Count / 2;
            for (int i = 0; i < digits.Count; i++)
            {
                if (i < waiDigit) waiGua += digits[i];
                else neiGua += digits[i];
            }

            waiGua--;
            neiGua--;
            waiGua %= 8;
            neiGua %= 8;

            //Console.WriteLine($"外卦{waiGua}");
            //Console.WriteLine($"内卦{neiGua}");

            var symbol = new HexSymbol(waiGua, neiGua);
            var symbolOriName = symbol.Name;
            // Console.WriteLine($"卦名{symbolOriName}");
            // 变卦
            var changeIndex = (digits.Sum() + GetHourIndex()) % 6;
            // Console.WriteLine($"数字按位求和{digits.Sum()}, 时辰{GetHourIndex()}");
            // Console.WriteLine($"变卦位置{changeIndex}");

            symbol.Change(changeIndex);
            var symbolChangedName = symbol.Name;
            // Console.WriteLine($"变卦名{symbolChangedName}");

            // 结果
            var result = new List<string>();

            var ori = _guaData[symbolOriName] ?? new JObject();
            var cha = _guaData[symbolChangedName] ?? new JObject();

            result.Add($"[{symbolOriName}] (本卦) - {JSONHelper.ParseString(ori.name)}");
            result.Add(string.Join("\n", JSONHelper.ParseStringList(ori.desc)));
            result.Add("");

            result.Add($"[{symbolChangedName}] (变卦) - {JSONHelper.ParseString(cha.name)}");
            result.Add(string.Join("\n", JSONHelper.ParseStringList(cha.desc)));
            result.Add("");

            ori = ori["yaoci"] ?? new JObject();
            ori = ori[changeIndex.ToString()] ?? new JObject();

            result.Add($"[{JSONHelper.ParseString(ori.name)}]");
            result.Add(string.Join("\n", JSONHelper.ParseStringList(ori.desc)));
            result.Add("");

            cha = cha["yaoci"] ?? new JObject();
            cha = cha[changeIndex.ToString()] ?? new JObject();

            var jx1 = JSONHelper.ParseInt(ori.jx);
            var jx2 = JSONHelper.ParseInt(cha.jx);
            result.Add($"卦象评估：{DivinationFinal(jx1, jx2)}");
            result.Add("");

            result.Add("（诚心求占则灵，一事不三占）");


            return string.Join("\n", result).Replace("&ldquo;", "")
                                            .Replace("&rdquo;", "")
                                            .Replace("<strong>", "")
                                            .Replace("</strong>", "");
        }

        /// <summary>
        /// 把一个数字转换为每一位的数组
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private static List<int> GetDigitList(int number)
        {
            var digitList = new List<int>();
            while (number > 0)
            {
                digitList.Add(number % 10);
                number /= 10;
            }
            digitList.Reverse();
            return digitList;
        }

        #region 卦
        private class HexSymbol
        {
            public int Upper { get; private set; }
            public int Lower { get; private set; }
            public string Name { get => $"{OctSymbolName(Upper)}{OctSymbolName(Lower)}"; }
            public static string OctSymbolName(int val) => val switch
            {
                0 => "乾",   // 0 0 0   |||
                1 => "兑",   // 0 0 1   :||
                2 => "离",   // 0 1 0   |:|
                3 => "震",   // 0 1 1   ::|
                4 => "巽",   // 1 0 0   ||:
                5 => "坎",   // 1 0 1   :|:
                6 => "艮",   // 1 1 0   |::
                _ => "坤",   // 1 1 1   :::
            };
            public HexSymbol(int upper, int lower)
            {
                Upper = upper;
                Lower = lower;
            }

            /// <summary>
            /// 变卦
            /// </summary>
            /// <param name="index"></param>
            public void Change(int index)
            {
                byte value = (byte)Lower;
                value <<= 3;
                value += (byte)(Upper);
                // 0 0 0 | 0 0 0
                // 下    | 上
                // 1 2 3 | 4 5 6 
                index = 7 - index;
                value = ReverseBit(value, (byte)index);

                Upper = 7 & value;
                Lower = value >> 3;
            }

            // 按指定位数取反，位数从小到大，从1开始
            private byte ReverseBit(byte value, byte index)
            {
                byte n = (byte)(1 << (index - 1));
                byte cur = (byte)(value & n);
                if (cur > 0) value &= (byte)~n;
                else value |= n;
                return value;
            }


            private void PrintBin(byte val)
            {




            }

        }



        #endregion


        #region 爬虫
        private static List<string> _titles = new List<string>
        {
            "初六爻辞",
            "初九爻辞",
            "六二爻辞",
            "六三爻辞",
            "六四爻辞",
            "六五爻辞",
            "九二爻辞",
            "九三爻辞",
            "九四爻辞",
            "九五爻辞",
            "上九爻辞",
            "上六爻辞",
        };
        public static dynamic GetData(string url)
        {
            var content = Encoding.UTF8.GetString(Tools.Network.Client.HTTPGet.Get(url));
            Init();
            dynamic ret = new JObject();

            var ids = new List<string>();

            foreach (var item in content.Split("\n"))
            {
                if (item.Contains("<p><a href=\"/zhouyi/yijing64/"))
                {
                    var id = item.Split("<p><a href=\"/zhouyi/yijing64/").Last().Split(".html\"").First();
                    if (!ids.Contains(id)) ids.Add(id);
                }
            }

            int index = 1;

            foreach (var id in ids)
            {
                var one = GetFromURL($"https://www.zhouyi.cc/zhouyi/yijing64/{id}.html");
                if (one is JObject)
                {
                    var gua = $"{JSONHelper.ParseString(one.up)}{JSONHelper.ParseString(one.down)}";
                    ret[gua] = one;
                }
                Thread.Sleep(1000);
                Console.WriteLine($"id={id}, {index}/{ids.Count}");
                index++;
            }

            IOHelper.SaveCSV(Tools.Common.Enums.CSVType.RobotData, ret, "zhouyi64");

            return ret;
        }
        public static dynamic GetFromURL(string url)
        {
            try
            {

                var content = Encoding.UTF8.GetString(Tools.Network.Client.HTTPGet.Get(url));
                var shangGua = "";
                var xiaGua = "";
                var index = 0;
                var name = "";

                var yuanwen = new List<string>();
                dynamic yaoci = new JObject();
                Match? match;
                Regex? regex;
                int yaoIndex = 1;
                var arr = content.Split("\n");
                for (int i = 0; i < arr.Length; i++)
                {
                    var line = arr[i];
                    if (line.Contains("gua_toptt "))
                    {
                        regex = _mainTitle;
                        match = regex.Match(line);
                        index = Convert.ToInt32(match.Groups[1].Value);
                        name = match.Groups[2].Value;
                        shangGua = match.Groups[3].Value;
                        xiaGua += match.Groups[4].Value;
                    }
                    else if (line.Contains($"{name}原文"))
                    {
                        do
                        {
                            i++;
                            if (MatchStrongTitle(arr[i]) == "白话文解释") yuanwen.Add("白话文解释");
                            else if (arr[i].StartsWith("<strong>")) break;
                            else yuanwen.Add(arr[i].Replace("<br />", "").Trim());
                        } while (true);

                        do
                        {
                            i++;
                            if (MatchStrongTitle(arr[i]) == "《断易天机》解") continue;
                            else if (arr[i].StartsWith("<strong>")) break;
                            else continue;
                        } while (true);

                        i--;

                        do
                        {
                            i++;
                            if (MatchStrongTitle(arr[i]) == "北宋易学家邵雍解") yuanwen.Add("北宋易学家邵雍解");
                            else if (arr[i].Contains("台湾国学大儒傅佩荣解")) break;
                            else if (arr[i].StartsWith("<strong>")) break;
                            else yuanwen.Add(arr[i].Replace("<br />", "").Trim());
                        } while (true);
                    }
                    else
                    {

                        var title = MatchStrongTitle(line);
                        if (!_titles.Contains(title)) continue;
                        yaoci[yaoIndex.ToString()] = new JObject();
                        yaoci[yaoIndex.ToString()].index = yaoIndex;
                        yaoci[yaoIndex.ToString()].name = title;

                        var yaoci_desc = new List<string>();
                        do
                        {
                            i++;
                            if (MatchStrongTitle(arr[i]) == "白话文解释" || arr[i] == "白话文解释<br />") yaoci_desc.Add("白话文解释");
                            else if (arr[i].StartsWith("北宋易学家邵雍解")) break;
                            else if (arr[i].StartsWith("台湾国学大儒傅佩荣解")) break;
                            else if (arr[i].StartsWith("<strong>")) break;
                            else yaoci_desc.Add(arr[i].Replace("<br />", "").Trim());
                        } while (true);

                        do
                        {
                            if (MatchStrongTitle(arr[i]) == "北宋易学家邵雍解" || arr[i].Contains("北宋易学家邵雍解"))
                            {
                                yaoci_desc.Add("北宋易学家邵雍解");
                                var jixiong = arr[i + 1];
                                yaoci_desc.Add(jixiong);
                                if (jixiong.StartsWith("吉")) yaoci[yaoIndex.ToString()].jx = 1;
                                else if (jixiong.StartsWith("平")) yaoci[yaoIndex.ToString()].jx = 0;
                                else if (jixiong.StartsWith("凶")) yaoci[yaoIndex.ToString()].jx = -1;
                                break;
                            }
                            i++;
                            if (i >= 1000) break;
                        } while (true);

                        yaoci[yaoIndex.ToString()].desc = JArray.FromObject(yaoci_desc);

                        if (title == "上九爻辞" || title == "上六爻辞") break;
                        yaoIndex++;
                    }



                }

                dynamic ret = new JObject();
                ret.yaoci = yaoci;
                ret.name = name;
                ret.index = index;
                ret.up = shangGua;
                ret.down = xiaGua;
                ret.desc = JArray.FromObject(yuanwen);
                return ret;
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteException(ex, "ZHOUYI_DOWNLOAD");
                return null;
            }
        }
        private static Regex _mainTitle = new Regex(@"周易第(\d+)卦_(\S+)\(.*\)_(\S+)上(\S+)下");
        private static Regex _strongTitle = new Regex(@"<strong>(\S+)</strong>");
        private static Regex _strongTitle2 = new Regex(@"<div class=""gualist tleft f14 lh25""> (\S+)<br />");
        private static Regex _pageUrl = new Regex(@"<p><a href=""(\S+)"" target=""_blank"">");
        private static string MatchStrongTitle(string line)
        {
            var regex = _strongTitle;
            var match = regex.Match(line);
            if (!match.Success)
            {
                regex = _strongTitle2;
                match = regex.Match(line);
                if (!match.Success) return "";
            }
            return match.Groups[1].Value;
        }

        public static void ConvertRESFile()
        {
            var data = System.IO.File.ReadAllText("J:\\Projects\\RS\\Snail\\JJJ\\RS.JJJ\\bin\\Debug\\net6.0\\OUT\\zhouyi64.json");
            dynamic jo = JObject.Parse(data);
            dynamic ret = new JObject();
            foreach (var gua in jo)
            {
                var guaName = gua.Name;
                var guaData = gua.Value;
                ret[guaName] = new JObject();
                foreach (var item in guaData)
                {
                    if (item.Name == "yaoci")
                    {
                        ret[guaName][item.Name] = new JObject();
                        var yaoci = item.Value;
                        foreach (var yaociItem in yaoci)
                        {
                            var yaociName = yaociItem.Name;
                            var yaociData = yaociItem.Value;
                            var index = JSONHelper.ParseString(yaociData.index);
                            ret[guaName][item.Name][index] = new JObject();
                            ret[guaName][item.Name][index]["name"] = yaociName;
                            foreach (var item2 in yaociData)
                            {
                                ret[guaName][item.Name][index][item2.Name] = item2.Value;
                            }
                        }
                    }
                    else ret[guaName][item.Name] = item.Value;
                }
            }

            IOHelper.SaveCSV(Tools.Common.Enums.CSVType.RobotData, ret, "zhouyi64");
        }
        #endregion
    }
}
