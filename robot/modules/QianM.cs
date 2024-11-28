using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.utils;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.modules
{
    internal class QianM : IModule
    {
        #region FIELDS
        public string ModuleName { get => "QianM"; }
        public Context _context { get; set; }

        private bool _inited = false;
        public bool Inited { get => _inited; }

        private static int QIAN_STAT_DEFAULT = 0;
        private static int QIAN_STAT_WAITING = 1;

        Context IModule._context => _context;

        private ConcurrentDictionary<string, (string shi, string jie)> _qians;
        private ConcurrentDictionary<string, (int stat, string qianKey, long time)> _qianCache;
        private ConcurrentDictionary<string, long> _guaCache;
        #endregion

        #region INIT
        public QianM(boot.Context context)
        {
            this._context = context;
        }
        public void Init(bool load = false)
        {
            if (_inited) return;
            _inited = true;
            LoadCSV();
            RegistBackup();
        }
        private void RegistBackup()
        {
            _context.BackupM.RegistSaveSessions(ModuleName, SaveCSV);
            _context.BackupM.RemoveBackupSession(Tools.Common.Enums.CSVType.RobotData, "qian_cache");
        }
        private void LoadCSV()
        {
            #region 求签
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BOT\\qian.txt");
            _qians = new ConcurrentDictionary<string, (string shi, string jie)>();
            if (System.IO.File.Exists(path))
            {
                var str = System.IO.File.ReadAllText(path).Replace("\r\n", "\n").Replace("\r", "\n");
                var lines = str.Split("\n");
                string line_title = "", line_subt = "", line_cont = "";
                for (int i = 0; i < lines.Length; i++)
                {
                    switch (i % 3)
                    {
                        case 0:
                            line_title = lines[i];
                            break;
                        case 1:
                            line_subt = lines[i];
                            break;
                        case 2:
                            line_cont = lines[i];
                            _qians[line_title] = (line_subt, line_cont);
                            break;
                        default:
                            continue;
                    }
                }

            }

            // 缓存
            _qianCache = new();
            var jo = IOHelper.GetCSV(Tools.Common.Enums.CSVType.RobotData, "qian_cache") ?? new JObject();
            foreach (var item in jo)
            {
                var wxid = item.Name;
                var data = item.Value;
                int stat = JSONHelper.ParseInt(data.stat);
                string key = JSONHelper.ParseString(data.key);
                long time = JSONHelper.ParseLong(data.time);
                _qianCache[wxid] = (stat, key, time);
            }
            #endregion

            #region 求卦
            DivinationHelper.Init();
            #endregion
        }

        private void SaveCSV()
        {

            try
            {
                dynamic jo = new JObject();
                foreach (var item in _qianCache)
                {
                    jo[item.Key] = JObject.FromObject(new
                    {
                        stat = item.Value.stat,
                        key = item.Value.qianKey,
                        time = item.Value.time
                    });
                }
                IOHelper.SaveCSV(Tools.Common.Enums.CSVType.RobotData, jo, "qian_cache");
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "CdM.SaveCSV");
            }
        }
        #endregion

        #region METHODS
        /// <summary>
        /// 求卦
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        public string QiuGua(string wxid, int number)
        {
            if (CheckGuaCD(wxid)) return "求卦冷却中...\n" +
                                         "每个小时只能求卦一次哦~";
            else if (number < 100) return "求卦需要你提供一个大于等于100的整数哦~";

            return DivinationHelper.GetDivination(number);
        }

        public string QiuQian(string wxid)
        {

            var stat = GetQianStat(wxid);
            if (stat.stat == QIAN_STAT_WAITING)
            {
                if (string.IsNullOrEmpty(stat.key)) return "求签冷却中...\n每天只能抽签一次哦~";
                else return "你刚刚已经抽过签了\n赶紧回复\"唧唧叽解签\"看看结果吧~";
            }

            //if (IsQianCD(wxid)) return "求签冷却中...\n" +
            //                    "每天只能抽签一次哦~";

            var rnd = new Random(DateTime.Now.Millisecond);
            string key = "";
            do
            {
                var idx = rnd.Next(0, _qians.Count);
                key = _qians.Keys.ToArray()[idx];
                if (key == stat.key) continue;
                else break;
            } while (true);

            SetQianStat(wxid, QIAN_STAT_WAITING, key);
            return $"您{RandomChou()}得[{key}]\n" +
                   $"{_qians[key].shi}\n" +
                   $"----------------" +
                   $"\n请发送\"唧唧叽解签\"查看解签。";
        }

        public string JieQian(string wxid)
        {
            var stat = GetQianStat(wxid);
            if (stat.stat != QIAN_STAT_WAITING)
            {
                if (IsQianCD(wxid)) return "求签冷却中...\n" +
                                           "每天只能求签一次哦~";
                else return "你还没有抽取新的签诗\n" +
                            "赶紧回复\"唧唧叽抽签\"试试看~";
            }
            var key = stat.key;
            if (!_qians.ContainsKey(key))
            {
                SetQianStat(wxid, QIAN_STAT_DEFAULT, "", true);
                return "很抱歉，抽签发生错误\n" +
                       "请你回复\"唧唧叽抽签\"重新抽一次~";

            }
            SetQianStat(wxid, QIAN_STAT_DEFAULT, key);
            return $"[{key}]\n\n" +
                   $"{_qians[key].shi}\n\n" +
                   _qians[key].jie;
        }
        #endregion

        #region PRIVATE
        private string RandomChou()
        {
            var list = new List<string> { "抽", "菗", "牰", "求", "莍" };
            return list[new Random().Next(0, list.Count)];
        }
        /// <summary>
        /// 求卦是否在冷却中
        /// </summary>
        /// <param name="wxid"></param>
        /// <returns></returns>
        private bool CheckGuaCD(string wxid)
        {
            if (_context.ContactsM.IsAdmin(wxid)) return false;
            var key = GetGuaCDKey(wxid);
            var time = _context.CdM.GetCache(key);
            var now = TimeHelper.ToTimeStamp();
            var isSameDuration = JJJ.Client.core.game.module.TimeM.IsSameDurationStatic(now, time, 3600);
            if (!isSameDuration) _context.CdM.SetCache(key, now);
            return isSameDuration;
        }


        /// <summary>
        /// 抽签在冷却中
        /// </summary>
        /// <param name="wxid"></param>
        /// <returns></returns>
        private bool IsQianCD(string wxid)
        {
            var now = TimeHelper.ToTimeStamp();
            var time = _qianCache.ContainsKey(wxid) ? _qianCache[wxid].time : 0;
            var isSameDuration = JJJ.Client.core.game.module.TimeM.IsSameDurationStatic(now, time, 86400);
            return isSameDuration;
        }

        private void SetQianStat(string wxid, int stat, string key = "", bool clear = false)
        {
            var now = TimeHelper.ToTimeStamp();
            if (!_qianCache.ContainsKey(wxid)) _qianCache[wxid] = (stat, key, clear ? 0 : now);
            else _qianCache[wxid] = (stat, string.IsNullOrEmpty(key) ? _qianCache[wxid].qianKey : key, clear ? 0 : now);
        }

        private (int stat, string key) GetQianStat(string wxid)
        {
            if (IsQianCD(wxid))
            {
                // CD中
                return (QIAN_STAT_WAITING, _qianCache.ContainsKey(wxid) ? _qianCache[wxid].qianKey : "");
            }
            else
            {
                return (QIAN_STAT_DEFAULT, "");
            }
        }

        private string GetGuaCDKey(string wxid) => $"gua_cd_{wxid}";
        #endregion
    }
}
