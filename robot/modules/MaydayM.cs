using Newtonsoft.Json.Linq;
using nng;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.utils;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RS.Snail.JJJ.robot.modules
{
    /// <summary>
    /// 五月天功能模块
    /// </summary>
    internal class MaydayM : IModule
    {
        #region FIELDS
        public string ModuleName { get => "MaydayM"; }
        public Context _context { get; set; }

        private bool _inited = false;
        public bool Inited { get => _inited; }
        Context IModule._context => _context;

        /// <summary>
        /// 注册的微信群
        /// </summary>
        private List<string> _maydayGroups;
        private object _banLockObject = new();
        /// <summary>
        /// 拉黑的用户id
        /// </summary>
        private List<string> _bannedWxids;
        /// <summary>
        /// cd记录
        /// </summary>
        private ConcurrentDictionary<string, MaydayCDs> _cds;
        private object _recipeIndexLock = new object();
        private int _recipeIndex = 0;

        /// <summary>
        /// 门票格式配置
        /// </summary>
        private dynamic _ticketFormats;
        /// <summary>
        /// 门票日期-格式对应
        /// </summary>
        private Dictionary<string, string> _ticketDates;
        /// <summary>
        /// 处方日期-格式对应
        /// </summary>
        private Dictionary<string, string> _recipeDates;

        private object _workingLockObject = new();
        private List<string> _workingWxids = new();
        #endregion

        #region INIT
        public MaydayM(boot.Context context)
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
            // _context.BackupM.RemoveBackupSession(Tools.Common.Enums.CSVType.RobotData, "qian_cache");
        }
        /// <summary>
        /// 重载买门票格式
        /// </summary>
        /// <returns></returns>
        public bool LoadTicketFormat()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RES\\mayday\\mayday_tickets_format.json");
            try
            {
                var data = File.ReadAllText(path);
                if (string.IsNullOrEmpty(data)) return false;
                _ticketFormats = JObject.Parse(data);
                _ticketDates = new();
                foreach (var item in _ticketFormats)
                {
                    var value = item.Value;

                    foreach (var item2 in value.date)
                    {
                        _ticketDates.Add(item2.Name, item.Name);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "MaydayM.LoadCSV(tickets)");
                return false;
            }
        }
        /// <summary>
        /// 重载处方格式
        /// </summary>
        /// <returns></returns>
        public bool LoadRecipeFormat()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RES\\mayday\\mayday_recipe_format.json");
            try
            {
                var data = File.ReadAllText(path);
                if (string.IsNullOrEmpty(data)) return false;
                _recipeDates = new();
                dynamic jo = JObject.Parse(data);
                foreach (var item in jo)
                {
                    _recipeDates.Add(item.Name, JSONHelper.ParseString(item.Value));
                }
                return true;
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "MaydayM.LoadCSV(recipies)");
                return false;
            }
        }
        private void LoadCSV()
        {
            #region 门票
            LoadTicketFormat();
            #endregion

            #region 处方
            LoadRecipeFormat();
            #endregion

            #region 记录及配置
            try
            {
                var data = IOHelper.GetCSV(Tools.Common.Enums.CSVType.Mayday);
                data = data ?? new JObject();
                _maydayGroups = JSONHelper.ParseStringList(data.groups) ?? new List<string>();
                _bannedWxids = JSONHelper.ParseStringList(data.bans) ?? new List<string>();
                _recipeIndex = JSONHelper.ParseInt(data.recipe_index);

                _cds = new ConcurrentDictionary<string, MaydayCDs>();
                if (data.cds is not null)
                {
                    foreach (var item in data.cds)
                    {
                        var wxid = item.Name;
                        var value = item.Value;
                        var lastTime = JSONHelper.ParseLong(value.lastTime);
                        var count = JSONHelper.ParseInt(value.count);
                        _cds.TryAdd(wxid, new MaydayCDs { Wxid = wxid, count = count, lastTime = lastTime });
                    }
                }
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "MaydayM.LoadCSV(configs)");
            }
            #endregion
        }

        private void SaveCSV()
        {

            try
            {
                var dic = new Dictionary<string, MaydayCDs>();
                foreach (var item in _cds)
                {
                    dic.Add(item.Key, item.Value);
                }
                dynamic save = JObject.FromObject(new
                {
                    groups = _maydayGroups,
                    bans = _bannedWxids,
                    recipe_index = _recipeIndex,
                    cds = dic,
                });
                IOHelper.SaveCSV(Tools.Common.Enums.CSVType.Mayday, save);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "MadayM.SaveCSV");
            }
        }
        #endregion

        #region METHODS
        /// <summary>
        /// 群是否被注册
        /// </summary>
        /// <param name="chatroom"></param>
        /// <returns></returns>
        public bool IsMaydayGroup(string chatroom) => _maydayGroups.Contains(chatroom);
        /// <summary>
        /// 添加五迷群
        /// </summary>
        /// <param name="chatroom"></param>
        /// <returns></returns>
        public (bool result, string desc) AddMaydayGroup(string chatroom)
        {
            if (_maydayGroups.Contains(chatroom)) return (false, "本群已经被添加过了！");
            _maydayGroups.Add(chatroom);
            return (true, "");
        }
        /// <summary>
        /// 移除五迷群
        /// </summary>
        /// <param name="chatroom"></param>
        /// <returns></returns>
        public (bool result, string desc) DelMaydayGroup(string chatroom)
        {
            if (!_maydayGroups.Contains(chatroom)) return (false, "本群还未被添加过！");
            _maydayGroups.Remove(chatroom);
            return (true, "");
        }
        public Dictionary<string, string> QueryMaydayGroupList()
        {
            var ret = new Dictionary<string, string>();
            foreach (var roomID in _maydayGroups)
            {
                var group = _context.ContactsM.FindGroup(roomID);
                if (group is null) continue;
                ret.Add(roomID, $"{group.Name}({group.Members.Count}人)");
            }
            return ret;
        }
        /// <summary>
        /// 门票列表
        /// </summary>
        /// <returns></returns>
        public List<string> QueryTicketList()
        {
            var ret = new List<string>();
            foreach (var item in _ticketDates)
            {
                ret.Add($"[{item.Key}] {item.Value}");
            }
            return ret;
        }
        public string QueryTicketInstru()
        {
            var ret = new List<string>
            {
                "门票模板字段说明:",
                "[日期] 必填，格式为YYYYMMDD",
                "[入口] 必填，如M区2层或西北门入口",
                "[区域] 必填，如253通道或场地A2区",
                "[座位] 必填，如7排8号",
                "[价格] 必填，如1855，格式为数字",
                "[观演人] 如Ringo",
                "[序号] 对应票面No.",
                "[票号] 对应票面Ticket No.",
                "[压缩] 填是(将图片压缩为ZIP发出)或否(直接发送图片)，默认为是",
                "[出血] 填是(保留5mm出血)或否，默认为否）",
                "门票打印建议:",
                "使用200g铜版纸正反面彩色打印",
                "门票尺寸宽20cm高8cm，若保留出血则上下左右各增加5mm"
            };
            return string.Join("\n", ret);
        }
        public string QueryTicketTemplate()
        {
            var ret = new List<string>
            {
                "请复制以下文本:",
                "---------START---------",
                "日期 20230603",
                "入口 西北门入口",
                "区域 场地A2区",
                "座位 7排 8号",
                "价格 1855",
                "观演人 Ringo",
                "----------END----------"
            };
            return string.Join("\n", ret);
        }
        public string QueryRecipeInstru()
        {
            var ret = new StringBuilder("处方模板字段说明:");
            ret.AppendLine("[日期] 必填，格式为YYYYMMDD");
            ret.AppendLine("[生日] 必填，格式为YYYYMMDD");
            ret.AppendLine("[姓名] 必填，如一杯茅铁，不能超过5个字");
            ret.AppendLine("处方打印建议:");
            ret.AppendLine("普通A4纸拼版打印2张");
            return ret.ToString();
        }
        public string QueryRecipeTemplate()
        {
            var ret = new StringBuilder("请复制以下文本：");
            ret.AppendLine("---------START---------");
            ret.AppendLine("日期 20230603");
            ret.AppendLine("生日 19990521");
            ret.AppendLine("姓名 一杯茅铁");
            ret.AppendLine("----------END----------");
            return ret.ToString();
        }

        public (bool result, string desc) CheckCDs(string wxid)
        {
            if (_context.ContactsM.IsAdmin(wxid)) return (true, "");

            if (!_cds.ContainsKey(wxid))
            {
                _cds[wxid] = new() { count = 1, lastTime = TimeHelper.ToTimeStamp(), Wxid = wxid };
                return (true, "");
            }
            else if (!_cds[wxid].IsLastTimeToday())
            {
                _cds[wxid].count = 1;
                _cds[wxid].lastTime = TimeHelper.ToTimeStamp();
                return (true, "");
            }
            else
            {
                if (_cds[wxid].count >= _context.ConfigsM.MaydayTicketCountPerday) return (false, $"你今天的生成次数已达到{_cds[wxid].count}次，请明天再来哦~");
                _cds[wxid].count++;
                _cds[wxid].lastTime = TimeHelper.ToTimeStamp();
                return (true, "");
            }
        }
        public int CountLeftToday(string wxid)
        {
            var left = _context.ConfigsM.MaydayTicketCountPerday;
            if (!_cds.ContainsKey(wxid) || !_cds[wxid].IsLastTimeToday()) return left;
            return left - _cds[wxid].count;
        }


        /// <summary>
        /// 输入门票生成字段
        /// </summary>
        /// <param name="text"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public (bool result, bool anyMarks, string conclusion) ImportTicketTokenFromText(string text, ref MaydayTicketToken token)
        {
            text = text.Trim().Replace("\r", "");
            if (string.IsNullOrEmpty(text)) return (false, false, "");
            var ret = true;
            var descs = new List<string>();
            var anyMarks = false;


            foreach (var l in text.Split("\n"))
            {
                var line = l.Trim();
                if (line.StartsWith(MaydayTicketToken.MarkDate))
                {
                    anyMarks = true;
                    var date = line.Substring(2).Trim();
                    if (!_ticketDates.ContainsKey(date))
                    {
                        ret = false;
                        descs.Add("\"你填写了错误的[日期]\n你可以回复\"门票列表\"并从中选择正确的八位数日期！\"");
                        continue;
                    }
                    token.Date = date;
                }
                else if (line.StartsWith(MaydayTicketToken.MarkGate))
                {
                    anyMarks = true;
                    var gate = line.Substring(2).Trim();
                    if (string.IsNullOrEmpty(gate)) continue;
                    token.Gate = gate;
                }
                else if (line.StartsWith(MaydayTicketToken.MarkArea))
                {
                    anyMarks = true;
                    var area = line.Substring(2).Trim();
                    if (string.IsNullOrEmpty(area)) continue;
                    token.Area = area;
                }
                else if (line.StartsWith(MaydayTicketToken.MarkPos))
                {
                    anyMarks = true;
                    var pos = line.Substring(2).Trim();
                    if (string.IsNullOrEmpty(pos)) continue;
                    token.Pos = pos;
                }
                else if (line.StartsWith(MaydayTicketToken.MarkPrice))
                {
                    anyMarks = true;
                    var price = line.Substring(2).Trim();
                    if (string.IsNullOrEmpty(price)) continue;
                    token.Price = price;
                }
                else if (line.StartsWith(MaydayTicketToken.MarkAudienceName))
                {
                    anyMarks = true;
                    var name = line.Substring(3).Trim();
                    if (!string.IsNullOrEmpty(name)) token.AudienceName = name;
                    else token.AudienceName = "WMLS";
                }
                else if (line.StartsWith(MaydayTicketToken.MarkNo))
                {
                    anyMarks = true;
                    var no = line.Substring(2).Trim();
                    if (!string.IsNullOrEmpty(no))
                    {
                        if (no.Length > 12) no = no.Substring(0, 12);
                        token.No = no;
                    }
                }
                else if (line.StartsWith(MaydayTicketToken.MarkTicketNo))
                {
                    anyMarks = true;
                    var ticketNo = line.Substring(2).Trim();
                    if (!string.IsNullOrEmpty(ticketNo))
                    {
                        if (ticketNo.Length > 10) ticketNo = ticketNo.Substring(0, 10);
                        token.TicketNo = ticketNo;
                    }
                }
                else if (line.StartsWith(MaydayTicketToken.MarkIsCompress))
                {
                    anyMarks = true;
                    var flag = line.Substring(2).Trim();
                    if (flag.Contains("否")) token.IsCompress = false;
                }
                else if (line.StartsWith(MaydayTicketToken.MarkIsBleed))
                {
                    anyMarks = true;
                    var flag = line.Substring(2).Trim();
                    if (flag.Contains("是")) token.IsBleed = false;
                }
            }

            if (descs.Count > 0) return (false, anyMarks, string.Join("\n", descs));
            return (true, anyMarks, "");
        }

        public (bool ret, string desc) CheckTicketToken(MaydayTicketToken token)
        {
            var ret = true;
            var descs = new List<string>();
            if (string.IsNullOrEmpty(token.Date)) descs.Add("[日期] 例如 20230603");
            if (string.IsNullOrEmpty(token.Gate)) descs.Add("[入口] 例如 西北门入口");
            if (string.IsNullOrEmpty(token.Area)) descs.Add("[区域] 例如 场地A2区");
            if (string.IsNullOrEmpty(token.Pos)) descs.Add("[座位] 例如 7排 8号");
            if (string.IsNullOrEmpty(token.Price)) descs.Add("[价格] 例如 1855");
            if (descs.Count > 0) return (false, $"还缺少以下必填项：\n{string.Join("\n", descs)}");

            if (string.IsNullOrEmpty(token.AudienceName)) token.AudienceName = "WMLS";

            return (true, "");
        }


        public List<string> DrawTicket(MaydayTicketToken token, Message msg)
        {
            List<string> files = RS.Snail.JJJ.utils.MaydayDrawing.DrawTicket(_ticketFormats[_ticketDates[token.Date]], _context.ContactsM.QueryGroupMemberNickForFile(msg.Sender, msg.RoomID), token) ?? new List<string>();
            return files.Where(x => System.IO.File.Exists(x)).ToList();
        }

        public (bool result, bool anyMarks, string conclusion) ImportRecipeTokenFromText(string text, ref MaydayTicketToken token)
        {
            text = text.Trim().Replace("\r", "");
            if (string.IsNullOrEmpty(text)) return (false, false, "");
            var ret = true;
            var descs = new List<string>();
            var anyMarks = false;
            foreach (var l in text.Split("\n"))
            {
                var line = l.Trim();
                if (line.StartsWith(MaydayTicketToken.MarkDate))
                {
                    anyMarks = true;
                    var date = line.Substring(2).Trim();
                    if (!_ticketDates.ContainsKey(date))
                    {
                        ret = false;
                        descs.Add("你填写了错误的[日期]\n请填写一个今年五月天开过的内地演唱会的八位数日期！");
                        continue;
                    }
                    token.Date = date;
                }
                else if (line.StartsWith(MaydayTicketToken.MarkBirthDay))
                {
                    anyMarks = true;
                    var birthday = line.Substring(2).Trim();
                    if (string.IsNullOrEmpty(birthday) || birthday.Length != 8)
                    {
                        ret = false;
                        descs.Add("你填写了错误的[生日]\n请填写一个八位数日期！");
                        continue;
                    }
                    token.BirthDay = birthday;
                }
                else if (line.StartsWith(MaydayTicketToken.MarkAudienceName))
                {
                    anyMarks = true;
                    var name = line.Substring(2).Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        descs.Add("[姓名]不能够为空");
                        ret = false;
                        continue;
                    }
                    else if (name.Length < 2)
                    {
                        descs.Add("[姓名]不能少于两个字");
                        ret = false;
                        continue;
                    }
                    else if (name.Length > 5)
                    {
                        descs.Add("[姓名]不能多于五个字或字母");
                        ret = false;
                        continue;
                    }
                    token.AudienceName = name;
                }
            }

            if (descs.Count > 0) return (false, anyMarks, string.Join("\n", descs));
            return (true, anyMarks, "");
        }


        public (bool ret, string desc) CheckRecipeToken(MaydayTicketToken token)
        {
            var ret = true;
            var descs = new List<string>();
            if (string.IsNullOrEmpty(token.Date)) descs.Add("[日期] 例如 20240526");
            if (string.IsNullOrEmpty(token.BirthDay)) descs.Add("[生日] 例如 19970329");
            if (string.IsNullOrEmpty(token.AudienceName)) descs.Add("[姓名] 例如 一杯茅铁");
            if (descs.Count > 0) return (false, $"还缺少以下必填项：\n{string.Join("\n", descs)}");

            return (true, "");
        }

        public List<string> DrawRecipe(MaydayTicketToken token, Message msg)
        {
            lock (_recipeIndexLock)
            {
                _recipeIndex++;
            }
            var files = RS.Snail.JJJ.utils.MaydayDrawing.DrawRecipe(_recipeIndex, _recipeDates[token.Date], _context.ContactsM.QueryGroupMemberNickForFile(msg.Sender, msg.RoomID), token) ?? new();
            return files.Where(x => System.IO.File.Exists(x)).ToList();
        }

        #region WORING WXID
        public bool IsWoring(string wxid)
        {
            lock (_workingLockObject)
            {
                return _workingWxids.Contains(wxid);
            }
        }
        public void AddWorking(string wxid)
        {
            lock (_workingLockObject)
            {
                _workingWxids.Add(wxid);
            }
        }
        public void RemoveWorking(string wxid)
        {
            lock (_workingLockObject)
            {
                _workingWxids.Remove(wxid);
            }
        }
        #endregion

        #region BANNED WXID
        public bool IsBanned(string wxid)
        {
            lock (_banLockObject)
            {
                return _bannedWxids.Contains(wxid);
            }
        }

        #endregion

        #endregion

        #region CDS

        private class MaydayCDs
        {
            public string Wxid { get; set; }
            public long lastTime { get; set; }
            public int count { get; set; }
            public bool IsLastTimeToday() => TimeHelper.IsSameDay(lastTime);
        }
        #endregion
    }
}
