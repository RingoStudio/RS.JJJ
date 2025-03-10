﻿using Aliyun.OSS;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.Drawing.Controls;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using Org.BouncyCastle.Ocsp;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.game.clone;
using RS.Snail.JJJ.Client.core.game.include;
using RS.Snail.JJJ.Client.core.game.module;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.cmd.misc;
using RS.Snail.JJJ.robot.include;
using RS.Snail.JJJ.utils;
using RS.Tools.Common.Enums;
using RS.Tools.Common.extension;
using RS.Tools.Common.Utils;
using RS.Tools.Excel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RS.Snail.JJJ.robot.modules
{
    /// <summary>
    /// 俱乐部数据模块
    /// </summary>
    internal class ClubsM : IModule
    {
        #region FIELDS
        public string ModuleName { get => "ClubsM"; }
        public Context _context { get; set; }

        private bool _inited = false;
        public bool Inited { get => _inited; }

        private ConcurrentDictionary<string, Club> _clubs;
        private ConcurrentDictionary<string, ClubMember> _clubMembers;
        #endregion

        #region INIT
        public ClubsM(boot.Context context)
        {
            this._context = context;
        }
        public void Init(bool load = false)
        {
            if (_inited) return;
            _inited = true;
            LoadCSV();
            RegistBackups();
        }
        /// <summary>
        ///  注册备份
        /// </summary>
        private void RegistBackups()
        {
            _context.BackupM.RegistSaveSessions(ModuleName, SaveCSV);
            _context.BackupM.RegistBackupSession(Tools.Common.Enums.CSVType.UserClub, include.files.Club_Data_CSV);
            _context.BackupM.RegistBackupSession(Tools.Common.Enums.CSVType.UserClub, include.files.Club_Members_CSV);
        }
        /// <summary>
        ///  加载CSV
        /// </summary>
        private void LoadCSV()
        {
            dynamic data;
            #region CLUBS
            try
            {
                data = IOHelper.GetCSV(Tools.Common.Enums.CSVType.UserClub, include.files.Club_Data_CSV) ?? new JObject();
                _clubs = new();
                foreach (var item in data)
                {
                    _clubs.TryAdd(item.Name, new Club(_context, item.Value));
                }
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, $"ClubsM.LoadCSV.CLUBS");
            }
            #endregion

            #region MEMBERS
            try
            {
                data = IOHelper.GetCSV(Tools.Common.Enums.CSVType.UserClub, include.files.Club_Members_CSV) ?? new JObject();
                _clubMembers = new();
                foreach (var item in data)
                {
                    _clubMembers.TryAdd(item.Name, new ClubMember(_context, item.Value));
                }
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, $"ClubsM.LoadCSV.MEMBERS");
            }
            #endregion
        }
        /// <summary>
        ///  保存CSV
        /// </summary>
        public void SaveCSV()
        {
            dynamic jo;
            try
            {
                jo = new JObject();
                foreach (var item in _clubs)
                {
                    jo[item.Key] = item.Value.GetJO();
                }
                IOHelper.SaveCSV(RS.Tools.Common.Enums.CSVType.UserClub, jo, include.files.Club_Data_CSV);

                Console.WriteLine(">> 已保存 Clubs");
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "ClubsM.SaveCSV.Clubs");
            }

            try
            {
                jo = new JObject();
                foreach (var item in _clubMembers)
                {
                    jo[item.Key] = item.Value.GetJO();
                }
                IOHelper.SaveCSV(RS.Tools.Common.Enums.CSVType.UserClub, jo, include.files.Club_Members_CSV);
                Console.WriteLine(">> 已保存 ClubMembers");
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "ClubsM.SaveCSV.ClubMembers");
            }
        }
        #endregion

        #region PUBLIC METHODS
        public bool IsClubGroupBinded(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return false;
            var group = _context.ContactsM.FindGroupByRID(rid);
            if (group is null) return false;
            return true;
        }
        /// <summary>
        /// 查找俱乐部
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public Club? FindClub(string rid)
        {
            if (!_clubs.ContainsKey(rid)) return null;
            return _clubs[rid];
        }
        /// <summary>
        /// 查找游戏角色
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public ClubMember? FindMember(string uid)
        {
            if (!_clubMembers.ContainsKey(uid)) return null;
            return _clubMembers[uid];
        }
        /// <summary>
        ///  查找俱乐部成员昵称
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public string? QueryMemberName(string uid)
        {
            var member = FindMember(uid);
            if (member is null) return null;
            return member.NameOrUID();
        }
        /// <summary>
        ///  查找俱乐部名称
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string? QueryClubName(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            return club.Name;
        }
        /// <summary>
        /// 查看俱乐部信息
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string? QueryClubInfo(string rid, bool isPrivate = false)
        {
            var club = FindClub(rid);
            if (club is null) return null;

            var ret = new List<string>();
            ret.Add($"绑定俱乐部: {club.Name} [{club.RID}]");

            var group = _context.ContactsM.FindGroupByRID(rid);
            if (group is not null)
            {
                // 会长
                var holder = _context.ContactsM.QueryClubHolderWXID(rid);
                if (string.IsNullOrEmpty(holder)) ret.Add($"会长: (空缺)");
                else ret.Add($"会长: {_context.ContactsM.QueryGroupMemberNick(holder, group.WXID)}");

                // 经理
                var managers = new List<string>();
                var uids = new List<string>();
                foreach (var member in group.Members)
                {
                    if (member.Value.Role == include.UserRole.GROUP_MANAGER)
                    {
                        managers.Add(member.Key);
                    }

                    if (member.Value.UIDs is not null)
                    {
                        foreach (var uid in member.Value.UIDs)
                        {
                            if (club.Members.Contains(uid) && !uids.Contains(uid))
                            {
                                uids.Add(uid);
                            }
                        }
                    }
                }


                if (managers.Count > 0) ret.Add($"经理({managers.Count}人): {string.Join(", ", managers.Select((a) => _context.ContactsM.QueryGroupMemberNick(a, group.WXID)))}");
                else ret.Add($"经理: (空缺)");

                // uid count
                ret.Add($"UID绑定数量: {uids.Count}/{club.Members.Count}");
                if (club.Level > 0) ret.Add($"成员数量: {uids.Count}/{club.MaxMemberNum} ({club.Level}级)");
            }

            ret.Add("");
            ret.Add($"俱乐部套装分配模式: {include.club.ClubKitPlanTypeDesc(club.ClubKitPlanType == ClubKitPlanType.UNDEFINED ? ClubKitPlanType.SONCOMBAT : club.ClubKitPlanType)}");
            ret.Add($"提醒内容模式: {(club.RemindContentNotCombine ? "分别@" : "合并@")}");
            ret.Add($"提醒群成员变动: {(club.DontRemindGroupMemberChanged ? "关闭" : "开启")}");
            ret.Add($"提醒俱乐部成员变动: {(club.DontRemindClubMemberChanged ? "关闭" : "开启")}");
            ret.Add("");

            ret.Add($"参与自动登录: {(club.LoginAuto ? "开启" : "关闭")}");
            if (club.AutoLoginSheetApplied)
            {
                ret.Add("自动登录模式: 自定义时间登录(按自动登录配置表)");
                ret.Add($"登录后自动提醒: {(club.RemindAfterLogin ? "开启" : "关闭")}");
                ret.Add(club.GetAutoLoginConfigsDesc());
                ret.Add($"手动登录后自动提醒物种历史: {(club.RemindContentContainsGroupWarHistory ? "开启" : "关闭")}");
                ret.Add($"手动登录后自动提醒挖矿: {(club.DontRemindMine ? "关闭" : "开启")}");
                ret.Add($"手动登录后自动提醒金银牌: {(club.DontRemindTokens ? "关闭" : "开启")}");
            }
            else
            {
                ret.Add("自动登录模式: 固定时间登录(每日12:30/18:00/20:00)");
                ret.Add($"登录后自动提醒: {(club.RemindAfterLogin ? "开启" : "关闭")}");
                ret.Add($"登录后提醒物种历史: {(club.RemindContentContainsGroupWarHistory ? "开启" : "关闭")}");
                ret.Add($"登录后提醒挖矿: {(club.DontRemindMine ? "关闭" : "开启")}");
                ret.Add($"登录后提醒金银牌: {(club.DontRemindTokens ? "关闭" : "开启")}");
            }


            if (club.LoginSimpleMode) ret.Add($"简易登录模式: 开启");
            if (isPrivate)
            {
                ret.Add($"物种事件分享密钥: {(string.IsNullOrEmpty(club.GroupwarEventShareKey) ? "(未设置)" : club.GroupwarEventShareKey)}");
                var purchaseEnd = "";
                var now = TimeHelper.ToTimeStamp();
                if (club.PurchaseEnd == long.MinValue) purchaseEnd = "永久";
                else if (now >= club.PurchaseEnd)
                {
                    if (club.PurchaseEnd <= now + 3 * 86400)
                    {
                        purchaseEnd = $"仅剩{TimeHelper.ChinsesTimeDurationDesc(club.PurchaseEnd - now)}";
                    }
                    else
                    {
                        purchaseEnd = TimeHelper.ChinsesTimeDesc(club.PurchaseEnd);
                    }
                }
                else
                {
                    purchaseEnd = $"已过期{TimeHelper.ChinsesTimeDurationDesc(now - club.PurchaseEnd)}";
                }
                ret.Add($"唧唧叽订阅有效期：{purchaseEnd}");
            }

            ret.Add("");
            ret.Add($"上次登录: {club.LastLoginDesc()}");

            return string.Join("\n", ret);
        }
        /// <summary>
        /// 处理登录后的数据
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="data"></param>
        public void LoadLoginData(string rid, dynamic data)
        {
            if (data is not JObject || JSONHelper.GetCount(data) == 0) return;

            var members = data.members ?? new JObject();
            // if (!_clubMembers.ContainsKey(robotWxid)) _clubMembers.TryAdd( new());
            foreach (var item in members)
            {
                string uid = item.Name;
                var memberData = item.Value;
                var memberName = JSONHelper.ParseString(memberData.nickname);
                if (!_clubMembers.ContainsKey(uid)) _clubMembers[uid] = new ClubMember(_context, uid, memberName);
                _clubMembers[uid].UpdateData(memberData);
            }

            // 更新俱乐部信息
            var club = data.club;
            if (club is not null)
            {
                //if (!_clubs.ContainsKey(robotWxid)) _clubs.TryAdd( new());
                /*if (_clubs.ContainsKey(rid))*/
                _clubs[rid].UpdateData(club);
                List<string> uids = JSONHelper.GetKeys(members);
                if (uids.Count > 0)
                {
                    var memberChange = _clubs[rid].UpdateMembers(uids);
                    foreach (var uid in memberChange.newMembers) ClubMemberChangeClub(uid, rid);
                    foreach (var uid in memberChange.removedMembers) ClubMemberChangeClub(uid, "");

                    // 提醒成员变动
                    if (!_clubs[rid].DontRemindClubMemberChanged)
                    {
                        var remindList = new List<string>();
                        if (memberChange.removedMembers.Count > 0)
                        {
                            remindList.Add($"发现以下 {memberChange.removedMembers.Count} 个成员退出本俱乐部:");
                            remindList.AddRange(memberChange.removedMembers.Select(a => QueryMemberName(a) ?? a));
                        }
                        if (memberChange.newMembers.Count > 0)
                        {
                            remindList.Add($"发现以下 {memberChange.newMembers.Count} 个成员新加入本俱乐部:");
                            remindList.AddRange(memberChange.newMembers.Select(a => QueryMemberName(a) ?? a));
                        }

                        if (remindList.Count > 0)
                        {
                            var msg = string.Join("\n", remindList);
                            var group = _context.ContactsM.FindGroupByRID(rid);
                            if (group is not null)
                            {
                                var holder = _context.ContactsM.QueryClubHolderWXID(rid);
                                if (msg.Length > 300)
                                {
                                    var fileName = $"OUT\\俱乐部成员变动通知_{group.Name}_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.txt";
                                    fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                                    System.IO.File.WriteAllText(fileName, msg);
                                    _context.WechatM.SendFile(fileName, group.WXID);
                                }
                                else
                                {
                                    if (_clubs[rid].LoginDontAtHolder || string.IsNullOrEmpty(holder))
                                    {
                                        _context.WechatM.SendText(msg, group.WXID);
                                    }
                                    else
                                    {
                                        _context.WechatM.SendAtText(msg, new List<string> { holder }, group.WXID);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 物种数据
            var gw = data.gw;
            if (gw is not null)
            {
                // 物种事件
                var events = gw.events;
                if (events is JObject)
                {
                    _context.GroupWarEventsM.Update(events, _clubs[rid].ChannelType, _clubs[rid].CombinedDistSort, _clubs[rid].GroupwarEventShareKey);
                    _clubs[rid].UpdateGWEvents(events);
                }
                _clubs[rid].UpdateGWData(gw.club ?? new JObject());
                var memberGW = gw.members ?? new JObject();
                foreach (var item in memberGW)
                {
                    string uid = item.Name;
                    var memberGWData = item.Value;
                    _clubMembers[uid].UpdateData(memberGWData, true);
                }
            }
        }
        public List<string> GetLoginRIDsWithConfigSheet(bool isAuto = false)
        {
            var ret = new List<string>();
            foreach (var item in _clubs)
            {
                if (item.Value.AccountPasswords is null || item.Value.AccountPasswords.Count <= 0) continue;
                if (!item.Value.IsPurchased()) continue;
                if (isAuto && !item.Value.LoginAuto) continue;
                if (!item.Value.AutoLoginSheetApplied) continue;
                var group = _context.ContactsM.FindGroupByRID(item.Key);
                if (group is null) continue;
                ret.Add(item.Key);
            }
            ret.Sort((a, b) => _clubs[b].LoginSort - _clubs[a].LoginSort);

            return ret;
        }
        /// <summary>
        /// 获取所有参与自动登录的俱乐部RID
        /// </summary>
        /// <returns></returns>
        public List<string> GetLoginRIDsWithoutConfigSheet(bool isAuto = false)
        {
            var ret = new List<string>();
            foreach (var item in _clubs)
            {
                if (item.Value.AccountPasswords is null || item.Value.AccountPasswords.Count <= 0) continue;
                if (!item.Value.IsPurchased()) continue;
                if (isAuto && !item.Value.LoginAuto) continue;
                if (item.Value.AutoLoginSheetApplied) continue;
                var group = _context.ContactsM.FindGroupByRID(item.Key);
                if (group is null) continue;
                ret.Add(item.Key);
            }
            ret.Sort((a, b) => _clubs[b].LoginSort - _clubs[a].LoginSort);

            return ret;
        }
        /// <summary>
        /// 查找俱乐部成员的WXID
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        public string? QueryMemberWxid(string rid, string uid)
        {
            var group = _context.ContactsM.FindGroupByRID(rid);
            if (group is null) return null;

            foreach (var member in group.Members)
            {
                if (member.Value.UIDs is not null && member.Value.UIDs.Contains(uid)) return member.Key;
            }

            return null;
        }
        #endregion

        #region CLUB MODIFICATIONS
        /// <summary>
        /// 增加俱乐部
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="channelType"></param>
        /// <returns></returns>
        public bool AddClub(string rid, ChannelType channelType)
        {
            try
            {
                if (_clubs.ContainsKey(rid)) return false;
                var club = new Club(_context, rid, channelType);
                _clubs.TryAdd(rid, club);
                return true;
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "ClubsM.AddClub");
            }
            return false;
        }
        /// <summary>
        /// 移除俱乐部
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public bool DelClub(string rid)
        {
            try
            {
                var club = FindClub(rid);
                if (club is null) return false;

                // 停止登录
                _context.SnailsM.StopLogin(rid);

                // 变更成员履历
                var uids = club.Members;
                foreach (var uid in uids)
                {
                    ClubMemberChangeClub(uid, "");
                }

                _clubs.Remove(rid, out _);
                return true;
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "ClubsM.DelClub");
            }
            return false;
        }
        public bool AddClubLoginAccount(string rid, string account, string password)
        {
            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password)) return false;
            var club = FindClub(rid);
            if (club is null) return false;

            club.AccountPasswords = club.AccountPasswords ?? new();
            club.AccountPasswords[account] = password;
            return true;
        }
        /// <summary>
        /// 移除俱乐部登录账号
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public (bool result, string? desc) DelClubLoginAccount(string rid, string account = "")
        {
            var club = FindClub(rid);
            if (club is null) return (false, $"没有找到俱乐部[{rid}]");

            if (!string.IsNullOrEmpty(account))
            {
                if (!club.AccountPasswords.ContainsKey(account)) return (false, $"俱乐部[{club.Name} {rid}]没有这个登录账号");
                club.AccountPasswords.Remove(account);
                return (true, $"已成功移除[{club.Name} {rid}]登录账号 {StringHelper.GetMixedPhoneNumber(account)}");
            }

            else
            {
                var count = club.AccountPasswords.Count;
                club.AccountPasswords.Clear();
                return (true, $"已成功移除[{club.Name} {rid}]的 {count} 个登录账号");
            }

        }

        public (bool result, string? desc) SetClubAutoLoginConfigSheet(string path, string rid)
        {
            var group = FindClub(rid);
            if (group is null) return (false, "未找到俱乐部。");
            if (!System.IO.File.Exists(path)) return (false, "配置文件未接收成功。");
            var checkTime = new Func<string, bool>(_time =>
            {
                var arr = _time.Split(":");
                if (arr.Length < 2) return false;
                foreach (var _item in arr)
                {
                    if (!StringHelper.IsInt(_item)) return false;
                }
                int hour = Convert.ToInt32(arr[0]), min = Convert.ToInt32(arr[1]), sec = arr.Length > 2 ? Convert.ToInt32(arr[2]) : 0;
                return hour >= 0 && hour <= 24 & min >= 0 && min <= 59 && sec >= 0 && sec <= 59;
            });
            var getWeekday = new Func<int, int>(_index => _index switch
            {
                0 or 1 or 2 => 6,
                3 or 4 or 5 => 7,
                6 or 7 or 8 => 1,
                9 or 10 or 11 => 2,
                12 or 13 or 14 => 3,
                15 or 16 or 17 => 4,
                18 => 5,
                _ => 0
            });
            try
            {
                var data = RS.Tools.Excel.ExcelHelper.ReadFistWorksheet(path, 4, 3, 19, 6);
                int cnt = 0;
                List<dynamic> list = new List<dynamic>();

                for (int i = 0; i < data.Count; i++)
                {
                    if (!checkTime(data[i][0])) continue;
                    dynamic jo = new JObject();
                    var weekday = getWeekday(i);
                    jo.weekday = weekday;
                    jo.index = (i % 3) + 1;
                    jo.time = data[i][0];
                    jo.isEnable = data[i][1] == "Y";
                    jo.remindTokens = data[i][2] == "Y" && weekday != 5;
                    jo.remindMines = data[i][3] == "Y" && weekday != 5;
                    jo.remindDrills = data[i][4] == "Y" && weekday != 5;
                    jo.remindGW = data[i][5] == "Y" && weekday != 5;
                    cnt++;
                    list.Add(jo);
                }

                if (cnt > 0)
                {
                    group.AutoLoginConfigs = new();
                    foreach (var item in list) group.AutoLoginConfigs.Add(new(item));
                    group.AutoLoginSheetApplied = true;
                    _context.SnailsM.RegistOneClubAutoLoginSchedule(rid);
                    return (true, $"已成功更新{group.Name}[{rid}]的{cnt}条自动登录配置");
                }
                else return (false, "传入的配置表中没有识别出任何有效的登录配置，操作失败！");
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        #endregion

        #region MEMBER MODIFICATIONS
        /// <summary>
        /// 俱乐部成员变更俱乐部
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="newRid"></param>
        public void ClubMemberChangeClub(string uid, string newRid = "")
        {
            var member = FindMember(uid);
            member?.ChangeClubRID(newRid);
        }
        #endregion

        #region CLUB INFO
        /// <summary>
        /// 获取俱乐部列表
        /// </summary>
        /// <returns></returns>
        public string GetClubListExcel()
        {
            try
            {
                var fileName = $"OUT\\{ExcelHelper.GetFileName(include.files.File_ClubStatements)}";
                var content = new List<List<string>>
                                    {
                                        new List<string>
                                        {
                                            "RID",
                                            "名称",
                                            "渠道",
                                            "绑定微信群",
                                            "登录账号数量",
                                            "登录顺序(Z-A)",
                                            "成员数量",
                                            "上一次登录",
                                            "提醒内容合并",
                                            "登录后自动提醒",
                                            "自动提醒包括物种历史",
                                            "自动提醒包括挖矿",
                                            "自动提醒包括金银牌",
                                            "物种事件共享密钥",
                                            "简易登录模式",
                                            "套装分配模式",
                                            "登录信息at会长",
                                            "区服名称",
                                            "大区",
                                            "合区数量",
                                            "初次订阅",
                                            "订阅到期",
                                        },
                                    };

                foreach (var club in _clubs.Values)
                {
                    content.Add(new List<string>
                    {
                        club.RID,
                        club.Name,
                        include.club.ChannelTypeDesc(club.ChannelType),
                        _context.ContactsM.FindGroupByRID(club.RID)?.Name??"未绑定",
                        club.AccountPasswords.Count.ToString(),
                        club.LoginSort.ToString(),
                        $"{club.Members.Count}",
                        club.LastLoginDesc(),
                        club.RemindContentNotCombine ? "关闭":"开启",
                        club.RemindAfterLogin ? "开启":"关闭",
                        club.RemindContentContainsGroupWarHistory ? "开启":"关闭",
                        club.DontRemindMine? "关闭":"开启",
                        club.DontRemindTokens?  "关闭":"开启",
                        club.GroupwarEventShareKey ??"",
                        club.LoginSimpleMode?"开启":"关闭",
                        include.club.ClubKitPlanTypeDesc(club.ClubKitPlanType == ClubKitPlanType.UNDEFINED ? ClubKitPlanType.SONCOMBAT : club.ClubKitPlanType),
                        club.LoginDontAtHolder ? "关闭":"开启",
                        club.DistDesc,
                        club.CombinedDistSort.ToString(),
                        club.DistCombine.ToString(),
                        TimeHelper.ChinsesTimeDesc(club.PurchaseStart),
                        TimeHelper.ChinsesTimeDesc(club.PurchaseEnd),
                    });
                }

                return ExcelHelper.SaveSingleWorksheet(content, fileName, "俱乐部名单");
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "ClubsM.GetClubListExcel");
                return "";
            }
        }
        #endregion

        #region GAME DATA
        #region GROUP WAR
        /// <summary>
        /// 查询物种总览EXCEL
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string? QueryGroupWarHistoryExcel(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;

            var fileName = $"OUT\\{ExcelHelper.GetFileName($"{include.files.File_GroupWarStatements}_{club.Name}_{rid}")}";
            var now = TimeHelper.ToTimeStamp();

            try
            {
                var content = new List<List<string>>();
                var header = new List<string>
                                {
                                    "UID",
                                    "昵称",
                                    "战力",
                                    "领导力",
                                    "演练实力",

                                    "本周活跃度",
                                    "游戏时长",
                                    "基因强度",

                                    "生命",
                                    "攻击",
                                    "防御",
                                    "追击",

                                    "艺术",
                                    "文化",
                                    "信仰",
                                    "人气",
                                    "科技",
                                    "五维总和",

                                    "实际套装",
                                    "指定套装",
                                    "物种分数",
                                    "挖矿到期",
                                    "挖矿剩余",
                                    "挖矿位置",
                                    "做事件次数",        // TODO
                                    "钻头已用",
                                    "钻头剩余",
                                    "银牌少领",
                                    "银牌本周少领",
                                    "银牌已用",
                                    "银牌剩余",
                                    "银牌礼包",
                                    "金牌少领",
                                    "金牌本周少领",
                                    "金牌已用",
                                    "金牌剩余",
                                    "金牌礼包",
                                    "挖矿加速",
                                };

                switch (club.Map)
                {
                    case "1":
                        header.AddRange(new List<string>
                        {
                            "蜣螂得分",
                            "蜣螂伤害",
                            "蜣螂总计少打(次)",
                            "蜣螂今日攻打(次)",
                        });
                        break;
                    case "2":
                        header.AddRange(new List<string>
                        {
                            "金鱼今日得分",
                            "金鱼今日耐久",
                            "金鱼今日少打",
                            "金鱼今日鞭尸",
                            "金鱼总计得分",
                            "金鱼总计耐久",
                            "金鱼总计少打",
                            "金鱼总计鞭尸",
                        });
                        break;
                    case "3":
                        header.AddRange(new List<string>
                        {
                            "北极贝今日开始时间",
                            "北极贝今日修建时长",
                            "北极贝今日开始太晚",
                            "北极贝总计修建时长",
                            "北极贝总计修建高度",
                            "北极贝修塔速度",
                            "北极贝总计使用卷轴",
                            "北极贝卷轴今日少领",
                            "北极贝卷轴今日少用",
                            "北极贝卷轴总计少领",
                            "北极贝卷轴总计少用",
                        });
                        break;
                    case "4":
                        header.AddRange(new List<string>
                        {
                            "仓鼠完成章节",
                            "仓鼠剩余骰子",
                            "仓鼠剩余红骰子",
                            "仓鼠使用骰子",
                            "仓鼠使用红骰子",
                            "仓鼠大宗交易次数",
                            "仓鼠领取补给时间",
                            "仓鼠提升建筑总等级",
                            "仓鼠拍卖地块",
                        });
                        break;
                    case "5":
                        header.AddRange(new List<string>
                        {
                            "螳螂个人积分",
                            "螳螂游历次数",
                            "螳螂所在擂台",
                            "螳螂修为",
                            "螳螂根骨",
                            "螳螂外劲",
                            "螳螂内息",
                            "螳螂架势",
                        });
                        break;
                }

                content.Add(header);

                foreach (var uid in club.Members)
                {
                    var member = FindMember(uid);
                    if (member is null) continue;

                    var kit = include.club.KitDesc(member.Kit);
                    var kitAlloc = include.club.KitDesc(member.KitAlloc);
                    if (kit != kitAlloc) kitAlloc = $"[red]{kitAlloc}";

                    var mineDest = JSONHelper.ParseLong(member.Query("mine_dest"));
                    var mineDestStr = mineDest <= 1000000000 ? "[red]不在矿中" : TimeHelper.ChineseTimeDescWithWeekday(mineDest);
                    var mineRemain = "";
                    if (mineDest <= 1000000000) mineRemain = "[red]不在矿中";
                    else if (mineDest < now) mineRemain = $"[red]超时{TimeHelper.ChinsesTimeDurationDesc(now - mineDest)}";
                    else if (mineDest < now + 1800) mineRemain = "[yellow]" + TimeHelper.ChinsesTimeDurationDesc(mineDest - now);
                    else mineRemain = TimeHelper.ChinsesTimeDurationDesc(mineDest - now);

                    var minePos = JSONHelper.ParseString(member.Query("mine_pos"));
                    if (string.IsNullOrEmpty(minePos)) minePos = "[red]不在矿中";

                    int drill = JSONHelper.ParseInt(member.Query("drill"));
                    int drillLack = JSONHelper.ParseInt(member.Query("drill_lack"));
                    int drillLeft = JSONHelper.ParseInt(member.Query("drill_total")) - drill;


                    var gold = JSONHelper.ParseInt(member.Query("token_gold"));
                    var goldLeft = JSONHelper.ParseInt(member.Query("token_gold_total")) - gold;
                    var goldLack = JSONHelper.ParseInt(member.Query("token_gold_lack_today"));
                    var goldLackTotal = JSONHelper.ParseInt(member.Query("token_gold_lack_total"));

                    var silver = JSONHelper.ParseInt(member.Query("token_silver"));
                    var silverLeft = JSONHelper.ParseInt(member.Query("token_silver_total")) - silver;
                    var silverLack = JSONHelper.ParseInt(member.Query("token_silver_lack_today"));
                    var silverLackTotal = JSONHelper.ParseInt(member.Query("token_silver_lack_total"));

                    var five = new Dictionary<string, long>();

                    foreach (var attrib in include.club.FiveAttribs)
                    {
                        five[attrib] = JSONHelper.ParseLong(member.Query(attrib));
                    }

                    var line = new List<string> {
                        uid,
                        member.Name,
                        JSONHelper.ParseLong(member.Query("combat")).ToString(),
                        JSONHelper.ParseInt(member.Query("leadership")).ToString(),
                        JSONHelper.ParseLong(member.Query("son_combat")).ToString(),

                        JSONHelper.ParseLong(member.Query("vitality")).ToString(),
                        JSONHelper.ParseLong(member.Query("login_days")).ToString(),
                        JSONHelper.ParseLong(member.Query("dna_impact")).ToString(),


                        JSONHelper.ParseLong(member.Query("max_hp")).ToString(),
                        JSONHelper.ParseLong(member.Query("attack")).ToString(),
                        JSONHelper.ParseLong(member.Query("defense")).ToString(),
                        JSONHelper.ParseLong(member.Query("combo")).ToString(),

                        five["charisma"].ToString(),
                        five["wit"].ToString(),
                        five["luck"].ToString(),
                        five["popularity"].ToString(),
                        five["knowledge"].ToString(),
                        five.Values.Sum().ToString(),

                        kit,
                        kitAlloc,
                        JSONHelper.ParseInt(member.Query("score")).ToString(),
                        mineDestStr,
                        mineRemain,
                        minePos,
                        JSONHelper.ParseInt(member.Query("event_multi_count")).ToString(),

                        drill.ToString(),
                        drillLeft.ToString(),

                        $"{(silverLack>0?"[red]":"")}{silverLack}",
                        $"{(silverLackTotal>0?"[red]":"")}{silverLackTotal}",
                        silver.ToString(),
                        $"{(silverLeft>0?"[yellow]":"")}{silverLeft}",
                        JSONHelper.ParseInt(member.Query("token_silver_buy"))>0?"已氪":"未氪",

                        $"{(goldLack>0?"[red]":"")}{goldLack}",
                        $"{(goldLackTotal>0?"[red]":"")}{goldLackTotal}",
                        gold.ToString(),
                        $"{(goldLeft>0?"[yellow]":"")}{goldLeft}",
                        JSONHelper.ParseInt(member.Query("token_gold_buy"))>0?"已氪":"未氪",

                        $"{(JSONHelper.ParseDouble(member.Query("mine_rate"))*100):N2}%"
                    };

                    if (club.Map == "1")
                    {
                        int todaySpe1 = JSONHelper.ParseInt(member.Query("gw/today_count"));
                        int lackSpe1 = JSONHelper.ParseInt(member.Query("gw/total_lack"));
                        int scoreSpe1 = JSONHelper.ParseInt(member.Query("gw/score"));
                        long damageSpe1 = JSONHelper.ParseLong(member.Query("gw/damage"));

                        line.AddRange(new List<string>
                                {
                                    scoreSpe1.ToString(),
                                    damageSpe1.ToString(),
                                    $"{(lackSpe1>0?"[red]":"")}{lackSpe1}",
                                    $"{(todaySpe1<1?"[red]":"")}{todaySpe1}",
                                });
                    }
                    else if (club.Map == "2")
                    {
                        int todayScore = JSONHelper.ParseInt(member.Query("gw/today_score"));
                        int todayDamage = JSONHelper.ParseInt(member.Query("gw/today_damage"));
                        int todayLack = JSONHelper.ParseInt(member.Query("gw/today_lack"));
                        int todayBs = JSONHelper.ParseInt(member.Query("gw/today_bs"));

                        int totalScore = JSONHelper.ParseInt(member.Query("gw/score"));
                        int totalDamage = JSONHelper.ParseInt(member.Query("gw/damage"));
                        int totalLack = JSONHelper.ParseInt(member.Query("gw/lack"));
                        int totalBs = JSONHelper.ParseInt(member.Query("gw/bs"));

                        line.AddRange(new List<string>
                                {
                                    todayScore.ToString(),
                                    todayDamage.ToString(),
                                    $"{(todayLack>0?"[red]":"")}{todayLack}",
                                    $"{(todayBs>0?"[red]":"")}{todayBs}",
                                    totalScore.ToString(),
                                    totalDamage.ToString(),
                                    $"{(totalLack>0?"[red]":"")}{totalLack}",
                                    $"{(totalBs>0?"[red]":"")}{totalBs}",
                                });
                    }
                    else if (club.Map == "3")
                    {
                        bool isStarted = JSONHelper.ParseBool(member.Query("gw/is_started"));
                        var startTime = JSONHelper.ParseLong(member.Query("gw/start"));
                        startTime = Math.Max(0, startTime);
                        var todayStart = (!isStarted || startTime <= 0) ? "[red]尚未开始" : TimeHelper.ChineseTimeDescWithWeekday(startTime);
                        var todayTime = (!isStarted || startTime <= 0) ? "[red]尚未开始" : TimeHelper.ChinsesTimeDurationDesc(JSONHelper.ParseLong(member.Query("gw/today_time")));
                        var late = "";
                        if (!isStarted) late = "[red]尚未开始";
                        else
                        {
                            var latePeriod = JSONHelper.ParseLong(member.Query("gw/today_time"));
                            if (latePeriod > 0) late = $"[red]{TimeHelper.ChinsesTimeDurationDesc(latePeriod)}";
                            else late = "";
                        }
                        var totalTime = TimeHelper.ChinsesTimeDurationDesc(JSONHelper.ParseLong(member.Query("gw/total_time")));
                        var height = JSONHelper.ParseInt(member.Query("gw/height"));
                        int costCount = JSONHelper.ParseInt(member.Query("gw/cost_count"));
                        int todayLack = JSONHelper.ParseInt(member.Query(club.Spe3DontNeedBuyGem ? "gw/lack_today_not_buy" : "gw/lack_today"));
                        int todayLeft = JSONHelper.ParseInt(member.Query("gw/not_use_today"));
                        int totalLack = JSONHelper.ParseInt(member.Query(club.Spe3DontNeedBuyGem ? "gw/lack_not_buy" : "gw/lack"));
                        int totalLeft = JSONHelper.ParseInt(member.Query("gw/not_use"));
                        double speed = JSONHelper.ParseDouble(member.Query("gw/rate"));

                        line.AddRange(new List<string>
                                {
                                    todayStart,
                                    todayTime,
                                    late,
                                    totalTime,
                                    height.ToString(),
                                    speed.ToString("0.00"),
                                    costCount.ToString(),
                                    $"{(todayLack>0?"[red]":"")}{todayLack}",
                                    $"{(todayLeft>0?"[red]":"")}{todayLeft}",
                                    $"{(totalLack>0?"[red]":"")}{totalLack}",
                                    $"{(totalLeft>0?"[red]":"")}{totalLeft}",
                                });
                    }
                    else if (club.Map == "4")
                    {
                        bool isSupply = !JSONHelper.ParseBool(member.Query("gw/not_supply"));
                        bool isAuction = !JSONHelper.ParseBool(member.Query("gw/lack_auction"));
                        var blockName = (((!club.Spe4DontNeedAuction) && isAuction) ? "" : "[red]") + JSONHelper.ParseString(member.Query("gw/block_name"));

                        var chapter = JSONHelper.ParseInt(member.Query("gw/chapter"));
                        var dice = JSONHelper.ParseInt(member.Query("gw/dice"));
                        var redDice = JSONHelper.ParseInt(member.Query("gw/red_dice"));
                        var bigDealCount = JSONHelper.ParseInt(member.Query("gw/big_deal_count"));
                        var supplyTime = isSupply ? TimeHelper.ChineseTimeDescWithWeekday(JSONHelper.ParseLong(member.Query("gw/supply_time"))) : "[red]今日未领取";
                        var upGrid = JSONHelper.ParseInt(member.Query("gw/up_grid"));
                        var costDice = JSONHelper.ParseInt(member.Query("gw/cost_dice"));
                        var costRedDice = JSONHelper.ParseInt(member.Query("gw/cost_red_dice"));
                        line.AddRange(new List<string>
                                {
                                    chapter < 0 ? "N/A" : chapter.ToString(),
                                    dice < 0 ? "N/A" : dice.ToString(),
                                    redDice < 0 ? "N/A" : redDice.ToString(),
                                    costDice < 0 ? "N/A" : costDice.ToString(),
                                    costRedDice < 0 ? "N/A" : costRedDice.ToString(),
                                    bigDealCount < 0 ? "N/A" : bigDealCount.ToString(),
                                    supplyTime,
                                    upGrid < 0 ? "N/A" : upGrid.ToString(),
                                    blockName,
                                });

                    }
                    else if (club.Map == "5")
                    {
                        var score = JSONHelper.ParseInt(member.Query("gw/socre"));
                        var count = JSONHelper.ParseInt(member.Query("gw/count"));
                        bool isLack = JSONHelper.ParseBool(member.Query("gw/lack"));
                        var grid = isLack ? "[red]未上擂台" : JSONHelper.ParseString(member.Query("gw/grid"));
                        var xiuwei = JSONHelper.ParseInt(member.Query("gw/xiuwei"));
                        var gengu = JSONHelper.ParseInt(member.Query("gw/gengu"));
                        var waijing = JSONHelper.ParseInt(member.Query("gw/waijing"));
                        var neixi = JSONHelper.ParseInt(member.Query("gw/neixi"));
                        var jiashi = JSONHelper.ParseInt(member.Query("gw/jiashi"));

                        line.AddRange(new List<string>
                        {
                            score.ToString(),
                            count.ToString(),
                            grid,
                            xiuwei.ToString(),
                            gengu.ToString(),
                            waijing.ToString(),
                            neixi.ToString(),
                            jiashi.ToString(),
                        });
                    }

                    content.Add(line);
                }

                return ExcelHelper.SaveSingleWorksheet(content, fileName, "物种总览");
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "ClubsM.QueryGroupWarHistoryExcel");
                return "";
            }
        }
        private int[] GWSpe4AuctionWeekdays = new int[] { 7, 1, 2, 3 };
        /// <summary>
        /// 查询BOSS伤害总览
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryGroupWarBossCombatRecordExcel(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsWeekUpdated()) return null;

            var fileName = $"OUT\\{ExcelHelper.GetFileName($"{include.files.File_BossDamageStatements}_{club.Name}_{rid}")}";

            try
            {
                var content = new List<List<string>>();
                var header = new List<string>
                                      {
                                        "区域",
                                        "UID",
                                        "昵称",
                                        "伤害值",
                                      };

                content.Add(header);

                var data = club.CombatRecord ?? new JObject();
                // {<area>:{<uid>:{"damage":<damage>}, ...}, ...}
                foreach (var areaItem in data)
                {
                    var area = areaItem.Name;
                    var areaData = areaItem.Value;
                    foreach (var memberItem in areaData)
                    {
                        var uid = memberItem.Name;
                        var damage = JSONHelper.ParseLong(memberItem.Value);

                        content.Add(new List<string>
                              {
                                area,
                                uid,
                                QueryMemberName(uid),
                                damage.ToString(),
                              });
                    }
                }

                return ExcelHelper.SaveSingleWorksheet(content, fileName, "BOSS伤害总览");
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "ClubsM.QueryGroupWarBossCombatRecordExcel");
                return "";
            }
        }
        /// <summary>
        /// 查询物种历史信息
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string? QueryGroupWarHistoryData(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;

            switch (club.Map)
            {
                case "1": return QueryGroupWarHisotySpe1(club);
                case "2": return QueryGroupWarHisotySpe2(club);
                case "3": return QueryGroupWarHisotySpe3(club);
                case "4": return QueryGroupWarHisotySpe4(club);
                case "5": return QueryGroupWarHisotySpe5(club);
                default: return null;
            }
        }
        /// <summary>
        /// 生成仓鼠历史拍卖Excel
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string? GetGroupWarSpe4AuctionExcel(string rid, bool isAdmin = false)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            var map = club.Map;
            if (map != "4") return null;

            var fileName = $"OUT\\{ExcelHelper.GetFileName($"{include.files.File_AuctionStatements}_{club.Name}_{rid}")}";

            try
            {
                var content = new Dictionary<string, List<List<string>>>
                {
                    { "拍卖结果", GetGroupWarSpe4AuctionResultData(club) },
                    { "参与俱乐部", GetGroupWarSpe4AuctionClubsData(club) },
                    { "拍卖地块", GetGroupWarSpe4AuctionBlocksData(club, isAdmin) },
                };

                return ExcelHelper.SaveMultiWorksheets(content, fileName);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "ClubsM.GetGroupWarSpe4AuctionExcel");
                return null;
            }

        }
        /// <summary>
        /// 获取仓鼠拍卖地块位置列表
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public List<string> GetGroupWarSpe4AuctionPoses(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            var data = club.GroupWarData?.dbase ?? new JObject();
            data = data[weekday.ToString()] ?? new JObject();
            return JSONHelper.GetKeys(data.blocks ?? new JObject());
        }
        /// <summary>
        /// 设置仓鼠拍卖地块
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="poses"></param>
        /// <returns></returns>
        public string SetGroupWarSpe4AuctionSetPoses(string rid, List<string> poses)
        {
            var club = FindClub(rid);
            if (club is null) return "未找到俱乐部";
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (!GWSpe4AuctionWeekdays.Contains(weekday)) return "今天不是拍卖的日子哦";
            if (club.Map != "4") return "这周不是仓鼠周哦";
            var now = TimeHelper.ToTimeStamp();

            club.GroupWarSpe4AuctionPoses = poses;
            club.GroupWarSpe4AuctionPosesUpdateTime = now;

            if (poses.Count == 0) return "已清空今天的仓鼠拍卖地块";
            return $"已将今天的仓鼠拍卖设置为 [{string.Join("、", poses)}]";
        }
        /// <summary>
        /// 获取仓鼠拍卖地块
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string GetGroupWarSpe4AuctionSetPoses(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return "未找到俱乐部";
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (!GWSpe4AuctionWeekdays.Contains(weekday)) return "今天不是拍卖的日子哦";
            if (club.Map != "4") return "这周不是仓鼠周哦";
            var now = TimeHelper.ToTimeStamp();

            if (!RS.Snail.JJJ.Client.core.game.module.TimeM.IsSameDurationStatic(now, club.GroupWarSpe4AuctionPosesUpdateTime, 86400)) return "今天还没设置仓鼠拍卖的位置哦";
            if (club.GroupWarSpe4AuctionPoses is null || club.GroupWarSpe4AuctionPoses.Count == 0) return "今天还没设置仓鼠拍卖的位置哦";

            return $"今天你要拍卖的地块是 [{string.Join("、", club.GroupWarSpe4AuctionPoses)}]";
        }
        #endregion

        #region TOKEN GOLD
        /// <summary>
        /// 查询金牌
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string? QueryGroupWarGoldToke(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";

            var desc = new List<string>();
            Dictionary<string, int> data;

            // 未领
            data = QueryGroupWarGoldTokenList(club, include.TokenQueryType.LACK);
            int lackMemberCount = 0, lackTodayMemberCount = 0, lackTotalCount = 0, lackTodayTotalCount = 0;
            foreach (var item in data)
            {
                var _lackTotal = item.Value / 100;
                var _lackToday = item.Value % 100;
                if (_lackToday > 0)
                {
                    lackTodayTotalCount += _lackToday;
                    lackTodayMemberCount++;
                }
                if (_lackTotal > 0)
                {
                    lackTotalCount += _lackTotal;
                    lackMemberCount++;
                }
            }
            if (lackTotalCount == 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都领完金令牌啦！");
            }
            else
            {
                if (lackTodayMemberCount > 0) desc.Add($"{include.emoji.HAN}今天共{lackTodayMemberCount}人共少领{lackTodayTotalCount}个金令牌");
                desc.Add($"{include.emoji.HAN}本周共{lackMemberCount}人共少领{lackTotalCount}个金令牌");
                //if (lackMemberCount <= 20)
                //{
                desc.Add($"{include.emoji.SHOUZHI}发送\"查询金牌未领\" 查看详情");
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒金牌\"或\"提醒领物资\" 立刻提醒");
                //}
                //else
                //{
                //    desc.Add($"{include.emoji.SHOUZHI}\"查询物种总览\" 查看详情");
                //}
            }
            desc.Add("");

            // 未用
            data = QueryGroupWarGoldTokenList(club, include.TokenQueryType.NOT_USE);
            int notUseCount = 0, notUseMemberCount = 0;
            foreach (var item in data)
            {
                if (item.Value > 0)
                {
                    notUseCount += item.Value;
                    notUseMemberCount++;
                }
            }
            if (notUseCount == 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都用完金令牌啦！");
            }
            else
            {
                desc.Add($"{include.emoji.HAN}共{notUseMemberCount}人共剩余{notUseCount}个金令牌");
                //if (notUseMemberCount <= 20)
                //{
                desc.Add($"{include.emoji.SHOUZHI}发送\"查询金牌未用\"或\"查询金牌剩余\" 查看详情");
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒用金牌\"或\"提醒清金牌\"或\"提醒清物资\" 立刻提醒");
                //}
                //else
                //{
                //    desc.Add($"{include.emoji.SHOUZHI}\"查询物种总览\" 查看详情");
                //}

            }
            desc.Add("");

            // 已氪
            data = QueryGroupWarGoldTokenList(club, include.TokenQueryType.BUY);
            int buyMemberCount = 0;
            foreach (var item in data)
            {
                if (item.Value > 0)
                {
                    buyMemberCount++;
                }
            }
            if (buyMemberCount == 0)
            {
                desc.Add($"{include.emoji.HAN}大家都没氪金令牌！");
            }
            else if (buyMemberCount < club.Members.Count)
            {
                desc.Add($"{include.emoji.ZAN}共{buyMemberCount}/{club.Members.Count}人氪了金令牌");
                desc.Add($"{include.emoji.SHOUZHI}发送\"查询金牌已氪\" 查看详情");
            }
            else
            {
                desc.Add($"{include.emoji.HAHA}大家都氪金令牌啦！");
            }
            desc.Add("");

            // 未氪
            data = QueryGroupWarGoldTokenList(club, include.TokenQueryType.NOT_BUY);
            int notBuyMemberCount = 0;
            foreach (var item in data)
            {
                if (item.Value > 0)
                {
                    notBuyMemberCount++;
                }
            }
            if (notBuyMemberCount > 0 && notBuyMemberCount < club.Members.Count)
            {
                desc.Add($"{include.emoji.HAN}共{notBuyMemberCount}/{club.Members.Count}人没氪金令牌");
                desc.Add($"{include.emoji.SHOUZHI}发送\"查询金牌未氪\" 查看详情");
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒氪金牌\" 立刻提醒");
            }


            return string.Join("\n", desc);
        }
        /// <summary>
        /// 查询金牌未领
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryGroupWarGoldTokeLack(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";

            var desc = new List<string>();
            Dictionary<string, int> data;
            var list = new List<string>();
            // 未领
            data = QueryGroupWarGoldTokenList(club, include.TokenQueryType.LACK);
            int lackMemberCount = 0, lackTodayMemberCount = 0, lackTotalCount = 0, lackTodayTotalCount = 0;
            foreach (var item in data)
            {
                var _lackTotal = item.Value / 100;
                var _lackToday = item.Value % 100;
                if (_lackToday > 0)
                {
                    lackTodayTotalCount += _lackToday;
                    lackTodayMemberCount++;
                }
                if (_lackTotal > 0)
                {
                    lackTotalCount += _lackTotal;
                    lackMemberCount++;
                }
                if (_lackToday > 0 || _lackTotal > 0)
                {
                    list.Add($"{emoji.ZHUYI}[{QueryMemberName(item.Key)}]今天少{_lackToday}个，本周少{_lackTotal}个");
                }
            }
            if (lackTotalCount == 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都领完金令牌啦！");
            }
            else
            {
                if (lackTodayMemberCount > 0) desc.Add($"今天共{lackTodayMemberCount}人共少领{lackTodayTotalCount}个金令牌");
                desc.Add($"本周共{lackMemberCount}人共少领{lackTotalCount}个金令牌");
                //if (lackMemberCount <= 20)
                //{
                desc.AddRange(list);
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒金牌\"或\"提醒领物资\" 立刻提醒");
                //}
                //else
                //{
                //    desc.Add($"人太多了，请按以下提示操作：");
                //    desc.Add($"{include.emoji.SHOUZHI}\"查询物种总览\" 查看详情");
                //}
            }
            return string.Join("\n", desc);
        }
        /// <summary>
        /// 查询金牌剩余
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryGroupWarGoldTokeNotUse(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";

            var desc = new List<string>();
            Dictionary<string, int> data;
            var list = new List<string>();
            // 未领
            data = QueryGroupWarGoldTokenList(club, include.TokenQueryType.NOT_USE);
            data = data.OrderByDescending(o => o.Value).ToDictionary(o => o.Key, p => p.Value);

            int notUseCount = 0, notUseMemberCount = 0;
            foreach (var item in data)
            {
                if (item.Value > 0)
                {
                    notUseCount += item.Value;
                    notUseMemberCount++;
                    list.Add($"{emoji.ZHUYI}[{QueryMemberName(item.Key)}]剩余{item.Value}个");
                }
            }
            if (notUseCount == 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都用完金令牌啦！");
            }
            else
            {

                desc.Add($"共{notUseMemberCount}人共剩余{notUseCount}个金令牌");
                //if (notUseMemberCount <= 20)
                //{
                desc.AddRange(list);
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒用金牌\"或\"提醒清金牌\"或\"提醒清物资\" 立刻提醒");
                //}
                //else
                //{
                //desc.Add($"人太多了，请按以下提示操作：");
                //desc.Add($"{include.emoji.SHOUZHI}\"查询物种总览\" 查看详情");
                //}

            }

            return string.Join("\n", desc);
        }
        /// <summary>
        /// 查询金牌已氪
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryGroupWarGoldTokeBuy(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";

            var desc = new List<string>();
            Dictionary<string, int> data;
            var list = new List<string>();
            // 未领
            data = QueryGroupWarGoldTokenList(club, include.TokenQueryType.BUY);
            int buyMemberCount = 0;
            foreach (var item in data)
            {
                if (item.Value > 0)
                {
                    buyMemberCount++;
                    list.Add($"{emoji.ZAN} {QueryMemberName(item.Key)}");
                }
            }
            if (buyMemberCount == 0)
            {
                desc.Add($"{include.emoji.HAN}大家都没氪金令牌！");
            }
            else if (buyMemberCount < club.Members.Count)
            {
                desc.Add($"共{buyMemberCount}/{club.Members.Count}人氪了金令牌");
                desc.AddRange(list);
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒氪金牌\" 立刻提醒");
            }
            else
            {
                desc.Add($"{include.emoji.HAHA}大家都氪金令牌啦！");
            }

            return string.Join("\n", desc);
        }
        /// <summary>
        /// 查询金牌未氪
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryGroupWarGoldTokeNotBuy(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";

            var desc = new List<string>();
            Dictionary<string, int> data;
            var list = new List<string>();
            // 未领
            data = QueryGroupWarGoldTokenList(club, include.TokenQueryType.NOT_BUY);
            int notBuyMemberCount = 0;
            foreach (var item in data)
            {
                if (item.Value > 0)
                {
                    notBuyMemberCount++;
                    list.Add($"{QueryMemberName(item.Key)}");
                }
            }
            if (notBuyMemberCount == 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都氪金令牌啦！");
            }
            else if (notBuyMemberCount < club.Members.Count)
            {
                desc.Add($"共{notBuyMemberCount}/{club.Members.Count}人没氪金令牌");
                desc.AddRange(list);
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒氪金牌\" 立刻提醒");
            }
            else
            {
                desc.Add($"{include.emoji.HAN}大家都没氪金令牌！");
            }

            return string.Join("\n", desc);
        }
        #endregion

        #region TOKEN SILVER
        /// <summary>
        /// 查询银牌
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public string? QueryGroupWarSilverToke(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";

            var desc = new List<string>();
            Dictionary<string, int> data;

            // 未领
            data = QueryGroupWarSilverTokenList(club, include.TokenQueryType.LACK);
            int lackMemberCount = 0, lackTodayMemberCount = 0, lackTotalCount = 0, lackTodayTotalCount = 0;
            foreach (var item in data)
            {
                var _lackTotal = item.Value / 100;
                var _lackToday = item.Value % 100;
                if (_lackToday > 0)
                {
                    lackTodayTotalCount += _lackToday;
                    lackTodayMemberCount++;
                }
                if (_lackTotal > 0)
                {
                    lackTotalCount += _lackTotal;
                    lackMemberCount++;
                }
            }
            if (lackTotalCount == 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都领完银令牌啦！");
            }
            else
            {
                if (lackTodayMemberCount > 0) desc.Add($"{include.emoji.HAN}今天共{lackTodayMemberCount}人共少领{lackTodayTotalCount}个银令牌");
                desc.Add($"{include.emoji.HAN}本周共{lackMemberCount}人共少领{lackTotalCount}个银令牌");
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒银牌\"或\"提醒领物资\" 立刻提醒");
            }
            desc.Add("");

            // 未用
            data = QueryGroupWarSilverTokenList(club, include.TokenQueryType.NOT_USE);
            int notUseCount = 0, notUseMemberCount = 0;
            foreach (var item in data)
            {
                if (item.Value > 0)
                {
                    notUseCount += item.Value;
                    notUseMemberCount++;
                }
            }
            if (notUseCount == 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都用完银令牌啦！");
            }
            else
            {
                desc.Add($"{include.emoji.HAN}共{notUseMemberCount}人共剩余{notUseCount}个银令牌");
                desc.Add($"{include.emoji.SHOUZHI}发送\"查询银牌未用\"或\"查询银牌剩余\" 查看详情");
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒用银牌\"或\"提醒清银牌\"或\"提醒清物资\" 立刻提醒");
            }
            desc.Add("");

            // 已氪
            data = QueryGroupWarSilverTokenList(club, include.TokenQueryType.BUY);
            int buyMemberCount = 0;
            foreach (var item in data)
            {
                if (item.Value > 0)
                {
                    buyMemberCount++;
                }
            }
            if (buyMemberCount == 0)
            {
                desc.Add($"{include.emoji.HAN}大家都没氪银令牌！");
            }
            else if (buyMemberCount <= 30)
            {
                desc.Add($"{include.emoji.HAHA}共{buyMemberCount}/{club.Members.Count}人氪了银令牌");
                desc.Add($"{include.emoji.SHOUZHI}发送\"查询银牌已氪\" 查看详情");
            }
            else if (buyMemberCount < club.Members.Count)
            {
                desc.Add($"{include.emoji.HAHA}共{buyMemberCount}/{club.Members.Count}人氪了银令牌");
                desc.Add($"{include.emoji.SHOUZHI}发送\"查询物种总览\" 查看详情");
            }
            else
            {
                desc.Add($"{include.emoji.HAHA}大家都氪银令牌啦！");
            }
            desc.Add("");

            // 未氪
            data = QueryGroupWarSilverTokenList(club, include.TokenQueryType.NOT_BUY);
            int notBuyMemberCount = 0;
            foreach (var item in data)
            {
                if (item.Value > 0)
                {
                    notBuyMemberCount++;
                }
            }
            if (notBuyMemberCount > 0 && notBuyMemberCount < club.Members.Count)
            {
                desc.Add($"{include.emoji.HAN}共{notBuyMemberCount}/{club.Members.Count}人没氪银令牌");
                desc.Add($"{include.emoji.SHOUZHI}发送\"查询银牌未氪\" 查看详情");
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒氪银牌\" 立刻提醒");
            }


            return string.Join("\n", desc);
        }
        /// <summary>
        /// 查询银牌未领
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryGroupWarSilverTokeLack(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";

            var desc = new List<string>();
            Dictionary<string, int> data;
            var list = new List<string>();
            // 未领
            data = QueryGroupWarSilverTokenList(club, include.TokenQueryType.LACK);
            int lackMemberCount = 0, lackTodayMemberCount = 0, lackTotalCount = 0, lackTodayTotalCount = 0;
            foreach (var item in data)
            {
                var _lackTotal = item.Value / 100;
                var _lackToday = item.Value % 100;
                if (_lackToday > 0)
                {
                    lackTodayTotalCount += _lackToday;
                    lackTodayMemberCount++;
                }
                if (_lackTotal > 0)
                {
                    lackTotalCount += _lackTotal;
                    lackMemberCount++;
                }
                if (_lackToday > 0 || _lackTotal > 0)
                {
                    list.Add($"{emoji.ZHUYI}[{QueryMemberName(item.Key)}]今天少{_lackToday}个，本周少{_lackTotal}个");
                }
            }
            if (lackTotalCount == 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都领完银令牌啦！");
            }
            else
            {
                if (lackTodayMemberCount > 0) desc.Add($"今天共{lackTodayMemberCount}人共少领{lackTodayTotalCount}个银令牌");
                desc.Add($"本周共{lackMemberCount}人共少领{lackTotalCount}个银令牌");
                //if (lackMemberCount <= 20)
                //{
                desc.AddRange(list);
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒银牌\"或\"提醒领物资\" 立刻提醒");
                //}
                //else
                //{
                //    desc.Add($"人太多了，请按以下提示操作：");
                //    desc.Add($"{include.emoji.SHOUZHI}\"查询物种总览\" 查看详情");
                //}
            }
            return string.Join("\n", desc);
        }
        /// <summary>
        /// 查询银牌剩余
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryGroupWarSilverTokeNotUse(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";

            var desc = new List<string>();
            Dictionary<string, int> data;
            var list = new List<string>();
            // 未领
            data = QueryGroupWarSilverTokenList(club, include.TokenQueryType.NOT_USE);
            data = data.OrderByDescending(o => o.Value).ToDictionary(o => o.Key, p => p.Value);
            int notUseCount = 0, notUseMemberCount = 0;
            foreach (var item in data)
            {
                if (item.Value > 0)
                {
                    notUseCount += item.Value;
                    notUseMemberCount++;
                    list.Add($"{emoji.ZHUYI}[{QueryMemberName(item.Key)}]剩余{item.Value}个");
                }
            }
            if (notUseCount == 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都用完银令牌啦！");
            }
            else
            {
                desc.Add($"共{notUseMemberCount}人共剩余{notUseCount}个银令牌");
                //if (notUseMemberCount <= 20)
                //{
                desc.AddRange(list);
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒用银牌\"或\"提醒清银牌\"或\"提醒清物资\" 立刻提醒");
                //}
                //else
                //{
                //    desc.Add($"人太多了，请按以下提示操作：");
                //    desc.Add($"{include.emoji.SHOUZHI}\"查询物种总览\" 查看详情");
                //}
            }
            return string.Join("\n", desc);
        }
        /// <summary>
        /// 查询银牌已氪
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryGroupWarSilverTokeBuy(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";

            var desc = new List<string>();
            Dictionary<string, int> data;
            var list = new List<string>();
            // 未领
            data = QueryGroupWarSilverTokenList(club, include.TokenQueryType.BUY);
            int buyMemberCount = 0;
            foreach (var item in data)
            {
                if (item.Value > 0)
                {
                    buyMemberCount++;
                    list.Add($"{emoji.ZAN} {QueryMemberName(item.Key)}");
                }
            }
            if (buyMemberCount == 0)
            {
                desc.Add($"{include.emoji.HAN}大家都没氪银令牌！");
            }
            else if (buyMemberCount < club.Members.Count)
            {
                desc.Add($"共{buyMemberCount}/{club.Members.Count}人氪了银令牌");
                desc.AddRange(list);
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒氪银牌\" 立刻提醒");
            }
            else
            {
                desc.Add($"{include.emoji.HAHA}大家都氪银令牌啦！");
            }

            return string.Join("\n", desc);
        }
        /// <summary>
        /// 查询银牌未氪
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryGroupWarSilverTokeNotBuy(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";

            var desc = new List<string>();
            Dictionary<string, int> data;
            var list = new List<string>();
            // 未领
            data = QueryGroupWarSilverTokenList(club, include.TokenQueryType.NOT_BUY);
            int notBuyMemberCount = 0;
            foreach (var item in data)
            {
                if (item.Value > 0)
                {
                    notBuyMemberCount++;
                    list.Add($"{QueryMemberName(item.Key)}");
                }
            }
            if (notBuyMemberCount == 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都氪银令牌啦！");
            }
            else if (notBuyMemberCount < club.Members.Count)
            {
                desc.Add($"共{notBuyMemberCount}/{club.Members.Count}人没氪银令牌");
                desc.AddRange(list);
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒氪银牌\" 立刻提醒");
            }
            else
            {
                desc.Add($"{include.emoji.HAN}大家都没氪银令牌！");
            }

            return string.Join("\n", desc);
        }
        #endregion

        #region DRILL
        /// <summary>
        /// 查询钻头
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryGroupWarDrill(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";

            var desc = new List<string>();
            Dictionary<string, int> data;

            // 未用
            data = QueryGroupWarDrillList(club, include.DrillQueryType.NOT_USE);
            data = data.OrderByDescending(o => o.Value).ToDictionary(o => o.Key, p => p.Value);
            int notUseCount = 0, notUseMemberCount = 0;
            foreach (var item in data)
            {
                if (item.Value > 0)
                {
                    notUseCount += item.Value;
                    notUseMemberCount++;
                }
            }
            if (notUseCount == 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都用完钻头啦！");
            }
            else
            {
                desc.Add($"{include.emoji.HAN}共{notUseMemberCount}人共剩余{notUseCount}个钻头");
                //if (notUseMemberCount <= 20)
                //{
                desc.Add($"{include.emoji.SHOUZHI}发送\"查询钻头未用\"或\"查询钻头剩余\" 查看详情");
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒钻头\"或\"提醒清物资\" 立刻提醒");
                //}
                //else
                //{
                //    desc.Add($"{include.emoji.SHOUZHI}\"查询物种总览\" 查看详情");
                //}
            }
            desc.Add("");

            // 未达标
            data = QueryGroupWarDrillList(club, include.DrillQueryType.LACK);
            int lackCount = 0, lackMemberCount = 0;
            foreach (var item in data)
            {
                if (item.Value > 0)
                {
                    lackCount += item.Value;
                    lackMemberCount++;
                }
            }
            if (lackCount == 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都达标啦！");
            }
            else
            {
                desc.Add($"{include.emoji.HAN}共{lackMemberCount}人距离达标共差{lackCount}个钻头");
                //if (lackMemberCount <= 20)
                //{
                desc.Add($"{include.emoji.SHOUZHI}发送\"查询钻头未达标\" 查看详情");
                //}
                //else
                //{
                // desc.Add($"{include.emoji.SHOUZHI}\"查询物种总览\" 查看详情");
                //}
            }

            return string.Join("\n", desc);
        }
        /// <summary>
        /// 查询钻头剩余
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryGroupWarDrillNotUse(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";

            var desc = new List<string>();
            Dictionary<string, int> data;
            var list = new List<string>();
            // 未领
            data = QueryGroupWarDrillList(club, include.DrillQueryType.NOT_USE);
            data = data.OrderByDescending(o => o.Value).ToDictionary(o => o.Key, p => p.Value);
            int notUseCount = 0, notUseMemberCount = 0;
            foreach (var item in data)
            {
                if (item.Value > 0)
                {
                    notUseCount += item.Value;
                    notUseMemberCount++;
                    list.Add($"{emoji.ZHUYI}[{QueryMemberName(item.Key)}]剩余{item.Value}个");
                }
            }
            if (notUseCount == 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都用完钻头啦！");
            }
            else
            {
                desc.Add($"{include.emoji.HAN}共{notUseMemberCount}人共剩余{notUseCount}个钻头");
                //if (notUseMemberCount <= 20)
                //{
                desc.AddRange(list);
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒钻头\"或\"提醒清物资\" 立刻提醒");
                //}
                //else
                //{
                //    desc.Add($"人太多了，请按以下提示操作：");
                //    desc.Add($"{include.emoji.SHOUZHI}\"查询物种总览\" 查看详情");
                //}
            }

            return string.Join("\n", desc);
        }
        /// <summary>
        /// 查询钻头未达标
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryGroupWarDrillLack(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";

            var desc = new List<string>();
            Dictionary<string, int> data;
            var list = new List<string>();
            // 未领
            data = QueryGroupWarDrillList(club, include.DrillQueryType.LACK);
            data = data.OrderByDescending(o => o.Value).ToDictionary(o => o.Key, p => p.Value);
            int lackCount = 0, lackMemberCount = 0;
            foreach (var item in data)
            {
                if (item.Value > 0)
                {
                    lackCount += item.Value;
                    lackMemberCount++;
                    list.Add($"{emoji.ZHUYI}[{QueryMemberName(item.Key)}]还差{item.Value}个");
                }
            }
            if (lackCount == 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都达标啦！");
            }
            else
            {
                desc.Add($"{include.emoji.HAN}共{lackMemberCount}人距离达标共差{lackCount}个钻头");
                //if (lackMemberCount <= 20)
                //{
                desc.AddRange(list);
                //}
                //else
                //{
                //    desc.Add($"人太多了，请按以下提示操作：");
                //    desc.Add($"{include.emoji.SHOUZHI}\"查询物种总览\" 查看详情");
                //}


            }

            return string.Join("\n", desc);
        }
        #endregion

        #region MINE
        /// <summary>
        /// 查询挖矿
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="near"></param>
        /// <returns></returns>
        public string QueryGroupWarMine(string rid, double near = 0.5)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";

            var desc = new List<string>();
            var data = QueryGroupWarMineDestList(club);
            near = Math.Max(near, 0.5);
            var nearSec = Convert.ToInt64(3600 * near);
            int nearCount = 0, exceedCount = 0, lackCount = 0;
            var now = TimeHelper.ToTimeStamp();
            foreach (var item in data)
            {
                if (item.Value <= 0) lackCount++;
                else if (item.Value < now) exceedCount++;
                else if (item.Value - now <= nearSec) nearCount++;
            }

            if (lackCount > 0)
            {
                desc.Add($"{emoji.YIWEN}共{lackCount}人正在跳矿或不在矿中");
                desc.Add($"{include.emoji.SHOUZHI}发送\"查询挖矿超时\" 查看详情");
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒挖矿\" 立即提醒");
            }
            if (exceedCount > 0)
            {
                if (desc.Count > 0) desc.Add("");
                desc.Add($"{emoji.BAOZHA}共{exceedCount}人已超时");
                desc.Add($"{include.emoji.SHOUZHI}发送\"查询挖矿超时\" 查看详情");
                desc.Add($"{include.emoji.SHOUZHI}发送\"提醒挖矿\" 立即提醒");
            }
            if (nearCount > 0)
            {
                if (desc.Count > 0) desc.Add("");
                desc.Add($"{emoji.NAOZHONG}共{nearCount}人将在{near:N2}小时内超时");
                desc.Add($"{include.emoji.SHOUZHI}发送\"查询挖矿到期\"或\"查询挖矿即将到期\" 查看详情");
            }

            if (desc.Count == 0) desc.Add("大家都在好好挖矿~");
            return string.Join("\n", desc);
        }
        /// <summary>
        /// 查询挖矿超时
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryGroupWarMineExceed(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";

            var desc = new List<string>();
            var data = QueryGroupWarMineDestList(club);
            int exceedCount = 0, lackCount = 0;
            var now = TimeHelper.ToTimeStamp();
            var listLack = new List<string>();
            var listExceed = new List<string>();
            foreach (var item in data)
            {
                if (item.Value <= 0)
                {
                    lackCount++;
                    listLack.Add($"{emoji.YIWEN} {QueryMemberName(item.Key)}");
                }
                else if (item.Value < now)
                {
                    listExceed.Add($"{emoji.BAOZHA} [{QueryMemberName(item.Key)}]超时{TimeHelper.ChinsesTimeDurationDesc(now - item.Value)}");
                    exceedCount++;
                }
            }
            if (lackCount > 0)
            {
                desc.Add($"共{lackCount}人正在跳矿或不在矿中");
                /*if (lackCount <= 20)*/
                desc.AddRange(listLack);
                //else
                //{
                //    desc.Add($"人太多了，请按以下提示操作：");
                //    desc.Add($"{include.emoji.SHOUZHI}\"查询物种总览\" 查看详情");
                //}
            }
            if (exceedCount > 0)
            {
                if (desc.Count > 0) desc.Add("");
                desc.Add($"共{exceedCount}人已超时");
                /*if (lackCount <= 20)*/
                desc.AddRange(listExceed);
                //else
                //{
                //    desc.Add($"人太多了，请按以下提示操作：");
                //    desc.Add($"{include.emoji.SHOUZHI}\"查询物种总览\" 查看详情");
                //}
            }

            if (desc.Count == 0) desc.Add("大家都在好好挖矿~");
            else
            {
                desc.Add("");
                desc.Add($"{include.emoji.SHOUZHI}\"提醒挖矿\" 立即提醒");
            }

            return string.Join("\n", desc);
        }
        /// <summary>
        /// 查询挖矿到期
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="near"></param>
        /// <returns></returns>
        public string QueryGroupWarMineNear(string rid, double near = 0.5)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";

            var desc = new List<string>();
            var data = QueryGroupWarMineDestList(club);
            near = Math.Max(near, 0.5);
            var nearSec = Convert.ToInt64(3600 * near);
            int nearCount = 0;
            var now = TimeHelper.ToTimeStamp();
            var list = new List<string>();
            foreach (var item in data)
            {
                if (item.Value >= now && item.Value - now <= nearSec)
                {
                    list.Add($"{QueryMemberName(item.Key)} → {TimeHelper.ChineseTimeDescWithWeekday(item.Value)}");
                    nearCount++;
                }
            }

            if (nearCount > 0)
            {
                if (desc.Count > 0) desc.Add("");
                desc.Add($"共{nearCount}人将在{near:N2}小时内超时");
                //if (nearCount <= 20)
                //{
                desc.AddRange(list);
                //}
                //else
                //{
                //    desc.Add($"人太多了，请按以下提示操作：");
                //    desc.Add($"{include.emoji.SHOUZHI}\"查询物种总览\" 查看详情");
                //}
            }

            if (desc.Count == 0) desc.Add("大家都在好好挖矿~");

            return string.Join("\n", desc);
        }
        /// <summary>
        /// 我的矿
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        public string? QueryGroupWarMineMine(string rid, Group group, string wxid)
        {
            var club = FindClub(rid);
            if (club is null) return null;

            if (group is null) return null;

            var groupMember = group.Members.ContainsKey(wxid) ? group.Members[wxid] : null;
            if (groupMember is null) return null;

            if (groupMember.UIDs is null || groupMember.UIDs.Count <= 0) return null;

            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (weekday == 5) return "周五就好好休息，别管挖矿啦。";

            var now = TimeHelper.ToTimeStamp();
            var desc = new List<string>();
            foreach (var uid in groupMember.UIDs)
            {
                var member = FindMember(uid);
                if (member is null) continue;

                string pos = JSONHelper.ParseString(member.Query("mine_pos"));

                var dest = JSONHelper.ParseLong(member.Query("mine_dest"));
                var destDesc = "";
                bool isFarDest = false;

                // 内容 [昵称]
                desc.Add($"[{member.NameOrUID()}]");

                if (dest == 0) destDesc = $"{emoji.YIWEN}正在跳矿或不在矿中";
                else if (dest <= now) destDesc = $"{emoji.BAOZHA}已超时{TimeHelper.ChinsesTimeDurationDesc(now - dest)}";
                else if (dest <= now + 86400) destDesc = $"{emoji.NAOZHONG}还剩{TimeHelper.ChinsesTimeDurationDesc(dest - now)}";
                else
                {
                    destDesc = $"到期时间{TimeHelper.ChineseTimeDescWithWeekday(dest)}";
                    isFarDest = true;
                }
                // 内容 正在跳矿或不在矿中/已超时/还剩
                if (isFarDest) desc.Add($"{emoji.RIGHT}{TimeHelper.ChineseTimeDescWithWeekday(dest)}");
                else { desc.Add(destDesc); desc.Add($"{emoji.RIGHT}{TimeHelper.ChineseTimeDescWithWeekday(dest)}"); }


                if (!string.IsNullOrEmpty(pos))
                {
                    desc.Add($"{emoji.LOCATION}位置：{pos}");
                    var others = QueryAtSameMinePos(club, pos);
                    if (others.Contains(uid)) others.Remove(uid);
                    if (others.Count == 0) desc.Add($"{emoji.OK}一人一矿");
                    else
                    {

                        desc.Add($"还有{string.Join("，", others.Select(a => QueryMemberName(a)))}等{others.Count}人在此矿");
                    }
                }
            }

            if (desc.Count == 0) return null;
            return $"查询结果如下：\n{string.Join("\n", desc)}";
        }
        #endregion

        #region EVENTS
        /// <summary>
        /// 查询未参与事件
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryGroupWarZeroMultiEvent(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";

            var desc = new List<string>();
            Dictionary<string, int> data;
            var list = new List<string>();
            // 未领
            data = QueryGroupWarMultiEventCountList(club, true);
            int lackMemberCount = 0;
            foreach (var item in data)
            {
                lackMemberCount++;
                list.Add($"{QueryMemberName(item.Key)}");
            }
            if (lackMemberCount == 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都参加过事件啦！");
            }
            else
            {
                desc.Add($"{include.emoji.HAN}共{lackMemberCount}人还未做过事件");
                //if (lackMemberCount <= 20)
                //{
                desc.AddRange(list);
                //}
                //else
                //{
                //    desc.Add($"人太多了，请按以下提示操作：");
                //    desc.Add($"{include.emoji.SHOUZHI}\"查询物种总览\" 查看详情");
                //}
            }

            return string.Join("\n", desc);
        }
        /// <summary>
        /// 生成矿点表
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string? GetGroupWarEventsMineImage(string rid)
        {
            try
            {
                var club = FindClub(rid);
                if (club is null) return null;

                var clubName = club.Name;
                var now = TimeHelper.ToTimeStamp();
                var duration = $"{TimeHelper.SimpleTimeDescJustDate(RS.Snail.JJJ.Client.core.game.module.TimeM.GetGWWeekStartTimeStatic(now))} - " +
                               $"{TimeHelper.SimpleTimeDescJustDate(RS.Snail.JJJ.Client.core.game.module.TimeM.GetGWWeekEndTimeStatic(now))}";
                var dist = $"{include.club.ChannelTypeDesc(club.ChannelType)} 第{club.CombinedDistSort}大区";

                var poses = new Dictionary<int, List<string>>();
                var events = _context.GroupWarEventsM.QueryEvents(club.ChannelType, club.CombinedDistSort, club.GroupwarEventShareKey, type: JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MINE);
                if (events.Count > 0)
                {
                    events.Sort((a, b) => a.Sort - b.Sort);
                    foreach (var item in events)
                    {
                        if (!poses.ContainsKey(item.Area)) poses.Add(item.Area, new List<string>());
                        poses[item.Area].Add($"[{item.Row:D5} 行 {item.Col:D5}列] {item.NameNumberLetter}");
                    }
                }

                return GWImageHelper.GetEventMineImage(duration: duration,
                                                     clubName: clubName,
                                                     distSort: dist,
                                                     poses: poses);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "ClubsM.GetClubKitPlanImage");
                return null;
            }
        }
        /// <summary>
        /// 生成事件表
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string? GetGroupWarEventsMultiImage(string rid)
        {
            try
            {
                var club = FindClub(rid);
                if (club is null) return null;

                var clubName = club.Name;
                var now = TimeHelper.ToTimeStamp();
                var duration = $"{TimeHelper.SimpleTimeDescJustDate(RS.Snail.JJJ.Client.core.game.module.TimeM.GetGWWeekStartTimeStatic(now))} - " +
                               $"{TimeHelper.SimpleTimeDescJustDate(RS.Snail.JJJ.Client.core.game.module.TimeM.GetGWWeekEndTimeStatic(now))}";
                var dist = $"{include.club.ChannelTypeDesc(club.ChannelType)} 第{club.CombinedDistSort}大区";

                var poses = new Dictionary<int, List<string>>();
                var events = _context.GroupWarEventsM.QueryEvents(club.ChannelType, club.CombinedDistSort, club.GroupwarEventShareKey, type: JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MULTI);
                if (events.Count > 0)
                {
                    events.Sort((a, b) => a.Sort - b.Sort);
                    foreach (var item in events)
                    {
                        if (!poses.ContainsKey(item.Area)) poses.Add(item.Area, new List<string>());
                        poses[item.Area].Add($"[{item.Row:D5} 行 {item.Col:D5}列] {item.Name}");
                    }
                }

                var singles = new List<string>();
                events = _context.GroupWarEventsM.QueryEvents(club.ChannelType, club.CombinedDistSort, club.GroupwarEventShareKey, type: JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_SINGLE);
                if (events.Count > 0)
                {
                    events.Sort((a, b) => a.Sort - b.Sort);
                    foreach (var item in events)
                    {
                        if (item.Name.Contains("卡片兑换") || item.Name.Contains("原质萃取")) continue;
                        singles.Add($"[{item.Area:D5}区{item.Row:D5}行{item.Col:D5}列] {item.Name}");
                    }
                }

                return GWImageHelper.GetEventMultiImage(duration: duration,
                                                      clubName: clubName,
                                                      distSort: dist,
                                                      poses: poses,
                                                      singles: singles);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "ClubsM.GetGroupWarEventsMultiImage");
                return null;
            }
        }
        /// <summary>
        /// 生成boss表
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string? GetGroupWarEventsBossImage(string rid)
        {
            try
            {
                var club = FindClub(rid);
                if (club is null) return null;

                var clubName = club.Name;
                var now = TimeHelper.ToTimeStamp();
                var duration = $"{TimeHelper.SimpleTimeDescJustDate(RS.Snail.JJJ.Client.core.game.module.TimeM.GetGWWeekStartTimeStatic(now))} - " +
                               $"{TimeHelper.SimpleTimeDescJustDate(RS.Snail.JJJ.Client.core.game.module.TimeM.GetGWWeekEndTimeStatic(now))}";
                var dist = $"{include.club.ChannelTypeDesc(club.ChannelType)} 第{club.CombinedDistSort}大区";

                var boss = new Dictionary<int, string>();
                var events = _context.GroupWarEventsM.QueryEvents(club.ChannelType, club.CombinedDistSort, club.GroupwarEventShareKey, type: JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_BOSS);
                if (events.Count > 0)
                {
                    events.Sort((a, b) => a.Sort - b.Sort);
                    foreach (var item in events)
                    {
                        boss[item.Area] = $"[{item.Row:D5}行{item.Col:D5}列] {item.Name}";
                    }
                }

                var monster = new Dictionary<int, List<string>>();
                events = _context.GroupWarEventsM.QueryEvents(club.ChannelType, club.CombinedDistSort, club.GroupwarEventShareKey, type: JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MONSTER);
                if (monster.Count > 0)
                {
                    events.Sort((a, b) => a.SortMark - b.SortMark);
                    foreach (var item in events)
                    {
                        if (!monster.ContainsKey(item.Area)) monster.Add(item.Area, new List<string>());
                        monster[item.Area].Add($"[{item.Row:D5}行{item.Col:D5}列] {item.NameMonster}");
                    }
                }

                return GWImageHelper.GetEventBossImage(duration: duration,
                                                      clubName: clubName,
                                                      distSort: dist,
                                                      monster: monster,
                                                      boss: boss);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "ClubsM.GetGroupWarEventsBossImage");
                return null;
            }
        }
        /// <summary>
        /// 生成事件总表
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string? GetGroupWarEventsSumImage(string rid)
        {
            try
            {
                var club = FindClub(rid);
                if (club is null) return null;

                var clubName = club.Name;
                var now = TimeHelper.ToTimeStamp();
                var duration = $"{TimeHelper.SimpleTimeDescJustDate(RS.Snail.JJJ.Client.core.game.module.TimeM.GetGWWeekStartTimeStatic(now))} - " +
                               $"{TimeHelper.SimpleTimeDescJustDate(RS.Snail.JJJ.Client.core.game.module.TimeM.GetGWWeekEndTimeStatic(now))}";
                var dist = $"{include.club.ChannelTypeDesc(club.ChannelType)} 第{club.CombinedDistSort}大区";

                var boss = new Dictionary<int, string>();
                var events = _context.GroupWarEventsM.QueryEvents(club.ChannelType, club.CombinedDistSort, club.GroupwarEventShareKey, type: JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_BOSS);
                if (events.Count > 0)
                {
                    events.Sort((a, b) => a.Sort - b.Sort);
                    foreach (var item in events)
                    {
                        boss[item.Area] = $"{item.Row:D5}.{item.Col:D5}";
                    }
                }

                var monster = new Dictionary<int, List<string>>();
                events = _context.GroupWarEventsM.QueryEvents(club.ChannelType, club.CombinedDistSort, club.GroupwarEventShareKey, type: JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MONSTER);
                if (events.Count > 0)
                {
                    events.Sort((a, b) => a.SortMark - b.SortMark);
                    foreach (var item in events)
                    {
                        if (!monster.ContainsKey(item.Area)) monster.Add(item.Area, new List<string>());
                        monster[item.Area].Add($"{item.NameMonster} {item.Row:D5}.{item.Col:D5}");
                    }
                }

                var multis = new Dictionary<int, List<string>>();
                events = _context.GroupWarEventsM.QueryEvents(club.ChannelType, club.CombinedDistSort, club.GroupwarEventShareKey, type: JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MULTI);
                if (events.Count > 0)
                {
                    events.Sort((a, b) => a.Sort - b.Sort);
                    foreach (var item in events)
                    {
                        if (!multis.ContainsKey(item.Area)) multis.Add(item.Area, new List<string>());
                        multis[item.Area].Add($"{item.Row:D5}.{item.Col:D5}");
                    }
                }

                var singles = new List<string>();
                events = _context.GroupWarEventsM.QueryEvents(club.ChannelType, club.CombinedDistSort, club.GroupwarEventShareKey, type: JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_SINGLE);
                if (events.Count > 0)
                {
                    events.Sort((a, b) => a.Sort - b.Sort);
                    foreach (var item in events)
                    {
                        if (item.Name.Contains("卡片兑换") || item.Name.Contains("原质萃取")) continue;
                        singles.Add($"[{item.Area:D5}区{item.Row:D5}行{item.Col:D5}列] {item.Name}");
                    }
                }

                var mines = new Dictionary<int, List<string>>();
                events = _context.GroupWarEventsM.QueryEvents(club.ChannelType, club.CombinedDistSort, club.GroupwarEventShareKey, type: JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MINE);
                if (events.Count > 0)
                {
                    events.Sort((a, b) => a.Sort - b.Sort);
                    foreach (var item in events)
                    {
                        if (!mines.ContainsKey(item.Area)) mines.Add(item.Area, new List<string>());
                        mines[item.Area].Add($"{item.Row:D5}.{item.Col:D5}");
                    }
                }

                return GWImageHelper.GetEvenSumImage(duration: duration,
                                                   clubName: clubName,
                                                   distSort: dist,
                                                   monster: monster.ToDictionary(a => a.Key, a => string.Join(" | ", a.Value)),
                                                   multis: multis.ToDictionary(a => a.Key, a => string.Join(" | ", a.Value)),
                                                   mines: mines.ToDictionary(a => a.Key, a => string.Join(" | ", a.Value)),
                                                   boss: string.Join(" | ", boss),
                                                   singles: string.Join(" | ", singles));
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "ClubsM.GetGroupWarEventsBossImage");
                return null;
            }
        }
        #endregion

        #region KIT
        /// <summary>
        /// 查询我的套装
        /// </summary>
        /// <param name="chatroom"></param>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public string? QueryMyKit(string chatroom, string wxid)
        {
            var user = _context.ContactsM.FindGroupMember(chatroom, wxid);
            if (user is null) return "";
            var uids = user.UIDs;
            if (uids is null || uids.Count == 0) return null;
            var ret = new List<string>();
            foreach (var uid in uids)
            {
                var member = FindMember(uid);
                if (member is null) continue;

                var kitLocked = member.KitLocked;
                if (kitLocked != Kit.UNDEFINED) ret.Add($"[{member.NameOrUID()}]  锁定套装[{include.club.KitDesc(kitLocked)}]，分配套装[{include.club.KitDesc(member.KitAlloc)}]，领取套装[{include.club.KitDesc(member.Kit)}]");
                else ret.Add($"[{member.NameOrUID()}] 分配套装[{include.club.KitDesc(member.KitAlloc)}]，领取套装[{include.club.KitDesc(member.Kit)}]");
            }

            if (ret.Count == 0) return null;
            return $"查询结果如下：\n{string.Join("\n", ret)}";
        }
        /// <summary>
        /// 查询领错套装
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string? QueryIncorrectKit(string rid)
        {
            var club = FindClub(rid);
            if (club is null) return null;
            if (!RS.Snail.JJJ.Client.core.game.module.TimeM.IsSameDurationStatic(TimeHelper.ToTimeStamp(), club.KitPlanTime, 7 * 86400)) return $"本周还没有分配过套装\n{include.emoji.SHOUZHI}\"生成布阵图\"立即分配套装";

            var desc = new List<(string content, long kitTime)>();
            foreach (var uid in club.Members)
            {
                var member = FindMember(uid);
                if (member is null) continue;
                if (member.Kit == Kit.UNDEFINED || member.Kit != member.KitAlloc)
                {
                    desc.Add(($"[{member.NameOrUID}] 分配套装[{include.club.KitDesc(member.KitAlloc)}]，领取套装[{include.club.KitDesc(member.Kit)}]", JSONHelper.ParseLong(member.Query("kit_time"))));
                }
            }

            if (desc.Count <= 0)
            {
                return "大家都领了正确的套装啦~";
            }
            else
            {
                //if (desc.Count > 20)
                //{
                //    return $"共{desc.Count}人没领或领错套装，人太多啦\n{include.emoji.SHOUZHI}\"查询物种总览\" 查看详情";
                //}
                //else
                //{
                desc.Sort((a, b) =>
                {
                    if (a.kitTime < b.kitTime) return -1;
                    else if (a.kitTime == b.kitTime) return 0;
                    else return 1;
                });
                return $"共{desc.Count}人没领或领错套装\n{string.Join("\n", desc)}";
                //}
            }
        }
        /// <summary>
        /// 设置锁定套装
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="uid"></param>
        /// <param name="kit"></param>
        /// <returns></returns>
        public (bool result, string? desc) SetClubMemberLockedKit(string rid, string uid, Kit kit)
        {
            var member = FindMember(uid);
            if (member is null) return (false, $"未找到成员 [{uid}]");
            var club = FindClub(rid);
            if (club is null) return (false, $"未找到俱乐部 [{rid}]");

            // 检查套装分配是否超出数量
            if (kit != Kit.UNDEFINED)
            {
                var count = 0;
                foreach (var _uid in club.Members)
                {
                    var _member = FindMember(_uid);
                    if (_member is null) continue;
                    if (_member.KitLocked == kit) count++;
                    if (count >= 25) return (false, $"你要设置的套装 [{include.club.KitDesc(kit)}] 在本俱乐部内已有25人锁定，请选择其他套装");
                }
            }
            member.KitLocked = kit;

            if (kit == Kit.UNDEFINED) return (true, $" [{member.NameOrUID()}] 已清除锁定套装设置\n" +
                                                    $"从下一次分派套装开始，该成员将根据相应数据排行分配套装。");
            else return (true, $" [{member.NameOrUID()}] 锁定套装 [{include.club.KitDesc(kit)}]\n" +
                               $"从下一次分派套装开始，该成员的套装将被锁定。");
        }
        /// <summary>
        /// 生成布阵图
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string? GetClubKitPlanImage(string rid, bool force = false)
        {
            try
            {
                var club = FindClub(rid);
                if (club is null) return null;

                CalcKitPlan(club, force);

                var clubName = club.Name;
                var race = club.Map;
                var now = TimeHelper.ToTimeStamp();
                var duration = $"{TimeHelper.SimpleTimeDescJustDate(RS.Snail.JJJ.Client.core.game.module.TimeM.GetGWWeekStartTimeStatic(now))} - " +
                               $"{TimeHelper.SimpleTimeDescJustDate(RS.Snail.JJJ.Client.core.game.module.TimeM.GetGWWeekEndTimeStatic(now))}";
                Dictionary<Kit, List<(string uid, int index)>> names = new();
                var locks = new List<string>();

                foreach (var uid in club.Members)
                {
                    var member = FindMember(uid);
                    if (member is null) continue;

                    var kit = member.KitAlloc;
                    if (!names.ContainsKey(kit)) names.Add(kit, new());

                    var memberName = member.NameOrUID();

                    if (member.KitLocked != Kit.UNDEFINED) memberName += " [已锁定]";
                    else if (club.KitImageShowValue) memberName += $" [{member.Query("kit_value")}]";

                    if (member.KitLocked != Kit.UNDEFINED) names[kit].Add((memberName, JSONHelper.ParseInt(member.Query("kit_index"))));
                    else names[kit].Add((memberName, JSONHelper.ParseInt(member.Query("kit_index"))));

                    if (member.KitLocked != Kit.UNDEFINED) locks.Add(memberName);
                }

                foreach (var item in names)
                {
                    item.Value.Sort((a, b) => b.index - a.index);
                }
                // 将names 转换为字典，Key不变，Value取uid列表
                string type = include.club.ClubKitPlanTypeDesc(club.ClubKitPlanType == ClubKitPlanType.UNDEFINED ? ClubKitPlanType.SONCOMBAT : club.ClubKitPlanType);


                return GWImageHelper.GetClubKitPlanImage(race: race,
                                                       type: type,
                                                       clubName: clubName,
                                                       duration: duration,
                                                       names: names.ToDictionary(x => x.Key, x => x.Value.Select(y => y.uid).ToList()),
                                                       locks: locks);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "ClubsM.GetClubKitPlanImage");
                return null;
            }
        }
        #endregion

        #region REMIND
        /// <summary>
        /// 提醒挖矿
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="near"></param>
        /// <returns></returns>
        public (bool result, string? desc) RemindGroupWarMine(string rid, string chatroom, string sender, double near = 0.5)
        {
            var ret = new List<(List<string> wxids, string content)>();
            var club = FindClub(rid);
            if (club is null) return (false, "未找到俱乐部");
            if (!club.IsTodayUpdated()) return (false, "今天还没更新过物种数据，请先登录一次再发起提醒");
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (weekday == 5) return (false, "周五就不要提醒啦~");

            near = Math.Max(near, 0.5);
            var data = QueryGroupWarMineRemind(club);
            if (data.Count > ClubRemidMaxCount(club)) return (false, "要发出的提醒太多，建议直接@所有人");

            var result = SendRemindContent(club, data, chatroom, sender);

            return (result, null);
        }
        /// <summary>
        /// 提醒金牌
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="chatroom"></param>
        /// <param name="sender"></param>
        /// <returns></returns>
        public (bool result, string? desc) RemindGroupWarTokenGold(string rid, string chatroom, string sender)
        {
            var ret = new List<(List<string> wxids, string content)>();
            var club = FindClub(rid);
            if (club is null) return (false, "未找到俱乐部");
            if (!club.IsTodayUpdated()) return (false, "今天还没更新过物种数据，请先登录一次再发起提醒");
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (weekday == 5) return (false, "周五就不要提醒啦~");

            var data = QueryGroupWarTokenGoldRemind(club, include.TokenQueryType.LACK);
            data = CombineRemindDic(data, QueryGroupWarTokenGoldRemind(club, include.TokenQueryType.NOT_USE), club.RemindContentNotCombine ? "" : $"{include.emoji.GOLD}金牌没领");
            if (data.Count > ClubRemidMaxCount(club)) return (false, "要发出的提醒太多，建议直接@所有人");

            var result = SendRemindContent(club, data, chatroom, sender);

            return (result, null);
        }
        /// <summary>
        /// 提醒氪金牌
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="chatroom"></param>
        /// <param name="sender"></param>
        /// <returns></returns>
        public (bool result, string? desc) RemindGroupWarTokenGoldNotBuy(string rid, string chatroom, string sender)
        {
            var ret = new List<(List<string> wxids, string content)>();
            var club = FindClub(rid);
            if (club is null) return (false, "未找到俱乐部");
            if (!club.IsTodayUpdated()) return (false, "今天还没更新过物种数据，请先登录一次再发起提醒");
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (weekday == 5) return (false, "周五就不要提醒啦~");

            var data = QueryGroupWarTokenGoldRemind(club, include.TokenQueryType.NOT_BUY);
            if (data.Count > ClubRemidMaxCount(club)) return (false, "要发出的提醒太多，建议直接@所有人");

            var result = SendRemindContent(club, data, chatroom, sender);

            return (result, null);
        }
        /// <summary>
        /// 提醒氪金牌
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="chatroom"></param>
        /// <param name="sender"></param>
        /// <returns></returns>
        public (bool result, string? desc) RemindGroupWarTokenGoldLeft(string rid, string chatroom, string sender)
        {
            var ret = new List<(List<string> wxids, string content)>();
            var club = FindClub(rid);
            if (club is null) return (false, "未找到俱乐部");
            if (!club.IsTodayUpdated()) return (false, "今天还没更新过物种数据，请先登录一次再发起提醒");
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (weekday == 5) return (false, "周五就不要提醒啦~");

            var data = QueryGroupWarTokenGoldRemind(club, include.TokenQueryType.NOT_USE);
            if (data.Count > ClubRemidMaxCount(club)) return (false, "要发出的提醒太多，建议直接@所有人");

            var result = SendRemindContent(club, data, chatroom, sender);

            return (result, null);
        }
        /// <summary>
        /// 提醒银牌
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="chatroom"></param>
        /// <param name="sender"></param>
        /// <returns></returns>
        public (bool result, string? desc) RemindGroupWarTokenSilver(string rid, string chatroom, string sender)
        {
            var ret = new List<(List<string> wxids, string content)>();
            var club = FindClub(rid);
            if (club is null) return (false, "未找到俱乐部");
            if (!club.IsTodayUpdated()) return (false, "今天还没更新过物种数据，请先登录一次再发起提醒");
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (weekday == 5) return (false, "周五就不要提醒啦~");

            var data = QueryGroupWarTokenSilverRemind(club, include.TokenQueryType.LACK);
            data = CombineRemindDic(data, QueryGroupWarTokenSilverRemind(club, include.TokenQueryType.NOT_USE), club.RemindContentNotCombine ? "" : $"{include.emoji.SILVER}银牌没领");
            if (data.Count > ClubRemidMaxCount(club)) return (false, "要发出的提醒太多，建议直接@所有人");

            var result = SendRemindContent(club, data, chatroom, sender);

            return (result, null);
        }
        /// <summary>
        /// 提醒氪银牌
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="chatroom"></param>
        /// <param name="sender"></param>
        /// <returns></returns>
        public (bool result, string? desc) RemindGroupWarTokenSilverNotBuy(string rid, string chatroom, string sender)
        {
            var ret = new List<(List<string> wxids, string content)>();
            var club = FindClub(rid);
            if (club is null) return (false, "未找到俱乐部");
            if (!club.IsTodayUpdated()) return (false, "今天还没更新过物种数据，请先登录一次再发起提醒");
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (weekday == 5) return (false, "周五就不要提醒啦~");

            var data = QueryGroupWarTokenSilverRemind(club, include.TokenQueryType.LACK);
            data = CombineRemindDic(data, QueryGroupWarTokenGoldRemind(club, include.TokenQueryType.NOT_USE));
            if (data.Count > ClubRemidMaxCount(club)) return (false, "要发出的提醒太多，建议直接@所有人");

            var result = SendRemindContent(club, data, chatroom, sender);

            return (result, null);
        }
        /// <summary>
        /// 提醒清银牌
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="chatroom"></param>
        /// <param name="sender"></param>
        /// <returns></returns>
        public (bool result, string? desc) RemindGroupWarTokenSilverLeft(string rid, string chatroom, string sender)
        {
            var ret = new List<(List<string> wxids, string content)>();
            var club = FindClub(rid);
            if (club is null) return (false, "未找到俱乐部");
            if (!club.IsTodayUpdated()) return (false, "今天还没更新过物种数据，请先登录一次再发起提醒");
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (weekday == 5) return (false, "周五就不要提醒啦~");

            var data = QueryGroupWarTokenSilverRemind(club, include.TokenQueryType.NOT_USE);
            data = CombineRemindDic(data, QueryGroupWarTokenGoldRemind(club, include.TokenQueryType.NOT_USE));
            if (data.Count > ClubRemidMaxCount(club)) return (false, "要发出的提醒太多，建议直接@所有人");

            var result = SendRemindContent(club, data, chatroom, sender);

            return (result, null);
        }
        /// <summary>
        /// 提醒钻头
        /// 周四提示未达标
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="chatroom"></param>
        /// <param name="sender"></param>
        /// <returns></returns>
        public (bool result, string? desc) RemindGroupWarDrill(string rid, string chatroom, string sender, int min = 0, string notice = "")
        {
            var ret = new List<(List<string> wxids, string content)>();
            var club = FindClub(rid);
            if (club is null) return (false, "未找到俱乐部");
            if (!club.IsTodayUpdated()) return (false, "今天还没更新过物种数据，请先登录一次再发起提醒");
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (weekday == 5) return (false, "周五就不要提醒啦~");
            min = weekday == 4 ? min : Math.Max(20, min);

            var data = QueryGroupWarDrillRemind(club, include.DrillQueryType.NOT_USE, min, notice);
            if (RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic() == 4) data = CombineRemindDic(data, QueryGroupWarDrillRemind(club, include.DrillQueryType.LACK));
            if (data.Count > ClubRemidMaxCount(club)) return (false, "要发出的提醒太多，建议直接@所有人");

            var result = SendRemindContent(club, data, chatroom, sender);

            return (result, null);
        }
        /// <summary>
        /// 提醒钻头未达标
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="chatroom"></param>
        /// <param name="sender"></param>
        /// <param name="notice"></param>
        /// <returns></returns>
        public (bool result, string? desc) RemindGroupWarDrillLack(string rid, string chatroom, string sender)
        {
            var ret = new List<(List<string> wxids, string content)>();
            var club = FindClub(rid);
            if (club is null) return (false, "未找到俱乐部");
            if (!club.IsTodayUpdated()) return (false, "今天还没更新过物种数据，请先登录一次再发起提醒");
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (weekday == 5) return (false, "周五就不要提醒啦~");


            if (club.DrillUseLimit1 <= 0 || club.DrillUseLimit2 <= 0) return (false, "您还没有设定俱乐部钻头达标数量。请发送\"设置钻头达标数量\"指令进行设定");
            var data = QueryGroupWarDrillRemind(club, include.DrillQueryType.LACK);
            if (data.Count > ClubRemidMaxCount(club)) return (false, "要发出的提醒太多，建议直接@所有人");

            var result = SendRemindContent(club, data, chatroom, sender);

            return (result, null);
        }
        /// <summary>
        /// 提醒物种历史
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="chatroom"></param>
        /// <param name="sender"></param>
        /// <returns></returns>
        public (bool result, string? desc) RemindGroupWarHistory(string rid, string chatroom, string sender)
        {
            var ret = new List<(List<string> wxids, string content)>();
            var club = FindClub(rid);
            if (club is null) return (false, "未找到俱乐部");
            if (!club.IsTodayUpdated()) return (false, "今天还没更新过物种数据，请先登录一次再发起提醒");
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (weekday == 5) return (false, "周五就不要提醒啦~");

            var data = QueryGroupWarHistoryRemind(club);
            if (data.Count > ClubRemidMaxCount(club)) return (false, "要发出的提醒太多，建议直接@所有人");

            var result = SendRemindContent(club, data, chatroom, sender);

            return (result, null);
        }
        /// <summary>
        /// 提醒清资源 周四可用
        /// 金牌、银牌、钻头、挖矿、历史
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="chatroom"></param>
        /// <param name="sender"></param>
        /// <param name="min"></param>
        /// <returns></returns>
        public (bool result, string? desc) RemindGroupWarUseResources(string rid, string chatroom, string sender)
        {
            var ret = new List<(List<string> wxids, string content)>();
            var club = FindClub(rid);
            if (club is null) return (false, "未找到俱乐部");
            if (!club.IsTodayUpdated()) return (false, "今天还没更新过物种数据，请先登录一次再发起提醒");
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (weekday != 4) return (false, "此命令仅周四可用，请谨慎使用");

            var data = new Dictionary<string, List<string>>();

            #region SILVER
            // 银牌未领
            var queryData = QueryGroupWarTokenSilverRemind(club, TokenQueryType.LACK);
            data = CombineRemindDic(data, queryData);

            // 银牌未用
            queryData = QueryGroupWarTokenSilverRemind(club, TokenQueryType.LACK);
            string excludeDesc = club.RemindContentNotCombine ? "" : $"{include.emoji.SILVER}银牌没领";
            data = CombineRemindDic(data, queryData, excludeDesc);
            #endregion

            #region GOLD
            // 金牌未领
            queryData = QueryGroupWarTokenGoldRemind(club, TokenQueryType.LACK);
            data = CombineRemindDic(data, queryData);

            // 金牌未用
            queryData = QueryGroupWarTokenGoldRemind(club, TokenQueryType.NOT_USE);
            excludeDesc = club.RemindContentNotCombine ? "" : $"{include.emoji.GOLD}金牌没领";
            data = CombineRemindDic(data, queryData, excludeDesc);
            #endregion

            #region DRILL
            //// 钻头未达标
            //queryData = QueryGroupWarDrillRemind(club, DrillQueryType.LACK);
            //data = CombineRemindDic(data, queryData);

            // 钻头未用
            queryData = QueryGroupWarTokenGoldRemind(club, TokenQueryType.NOT_USE);
            excludeDesc = club.RemindContentNotCombine ? "" : $"{include.emoji.GOLD}金牌没领";
            data = CombineRemindDic(data, queryData, excludeDesc);
            #endregion

            // 挖矿
            queryData = QueryGroupWarMineRemind(club);
            data = CombineRemindDic(data, queryData);

            // 历史
            queryData = QueryGroupWarHistoryRemind(club);
            data = CombineRemindDic(data, queryData);
            var result = SendRemindContent(club, data, chatroom, sender);

            return (result, null);
        }
        /// <summary>
        /// 提醒领资源
        /// 金牌、银牌、历史
        /// 仅周四可用
        /// 数量无限制
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="chatroom"></param>
        /// <param name="sender"></param>
        /// <param name="min"></param>
        /// <returns></returns>
        public (bool result, string? desc) RemindGroupWarGetResources(string rid, string chatroom, string sender)
        {
            var ret = new List<(List<string> wxids, string content)>();
            var club = FindClub(rid);
            if (club is null) return (false, "未找到俱乐部");
            if (!club.IsTodayUpdated()) return (false, "今天还没更新过物种数据，请先登录一次再发起提醒");
            //var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            //if (weekday != 4) return (false, "此命令仅周四可用，请谨慎使用");

            var data = QueryGroupWarTokenSilverRemind(club, TokenQueryType.LACK);
            data = CombineRemindDic(data, QueryGroupWarTokenGoldRemind(club, TokenQueryType.LACK));
            data = CombineRemindDic(data, QueryGroupWarHistoryRemind(club));

            if (data.Count > ClubRemidMaxCount(club)) return (false, "要发出的提醒太多，建议直接@所有人");

            var result = SendRemindContent(club, data, chatroom, sender);

            return (result, null);
        }
        /// <summary>
        /// 登录后提醒
        /// 金牌、银牌、钻头、挖矿、历史
        /// 数量无限制
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="chatroom"></param>
        /// <param name="sender"></param>
        /// <param name="min"></param>
        /// <returns></returns>
        public (bool result, string? desc) RemindAfterLogin(string rid, string chatroom, string sender, Client.core.boot.Configuration config)
        {
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            var hour = DateTime.Now.Hour;

            var ret = new List<(List<string> wxids, string content)>();
            var club = FindClub(rid);
            if (club is null) return (false, "未找到俱乐部");
            if (!club.IsTodayUpdated()) return (false, "今天还没更新过物种数据，请先登录一次再发起提醒");
            Dictionary<string, List<string>> data = new();

            if (config.RemindTokens)
            {
                if (weekday == 4) data = QueryGroupWarTokenSilverRemind(club, TokenQueryType.NOT_USE);
                data = CombineRemindDic(data, QueryGroupWarTokenSilverRemind(club, TokenQueryType.LACK));
                if (weekday == 4) data = CombineRemindDic(data, QueryGroupWarTokenGoldRemind(club, TokenQueryType.NOT_USE));
                data = CombineRemindDic(data, QueryGroupWarTokenGoldRemind(club, TokenQueryType.LACK));
            }

            if (config.RemindMines)
            {
                data = CombineRemindDic(data, QueryGroupWarMineRemind(club));
            }

            if (config.RemindDrills)
            {
                data = CombineRemindDic(data, QueryGroupWarDrillRemind(club, include.DrillQueryType.NOT_USE));
                data = CombineRemindDic(data, QueryGroupWarDrillRemind(club, include.DrillQueryType.LACK));
            }

            if (config.RemindGW)
            {
                data = CombineRemindDic(data, QueryGroupWarHistoryRemind(club, true));
            }

            // if (data.Count > ClubRemidMaxCount(club)) return (false, "要发出的提醒太多，建议直接@所有人");

            var result = SendRemindContent(club, data, chatroom, sender, true);

            return (result, null);
        }
        #endregion

        #endregion

        #region PRIVATE GAME DATA
        /// <summary>
        /// 查询蜣螂历史（文本）
        /// </summary>
        /// <param name="club"></param>
        /// <returns></returns>
        private string? QueryGroupWarHisotySpe1(Club club)
        {
            // 物种数据在哪？
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (weekday == 5) return "周五就不用查物种历史啦";

            int todayLack = 0;
            int totalLack = 0;
            int totalLackMember = 0;
            var list = new List<string>();

            foreach (var uid in club.Members)
            {
                var member = FindMember(uid);
                if (member is null) continue;

                // score
                // damage
                // count
                // today_count
                var _list = new List<string>();
                if (JSONHelper.ParseInt(member.Query("gw/today_count")) < 1)
                {
                    todayLack++;
                    _list.Add($"今天没打");
                }
                var lack = JSONHelper.ParseInt(member.Query("gw/total_lack"));
                if (lack > 0)
                {
                    _list.Add($"共少{lack}次");
                    totalLack += lack;
                    totalLackMember++;
                }
                if (_list.Count > 0) list.Add($"{include.emoji.ZHUYI}[{member.NameOrUID()}]{string.Join("，", _list)}");
            }

            var desc = new List<string>
            {
                $"---- 蜣螂历史 ----"
            };
            if (todayLack == 0) desc.Add($"{include.emoji.HAHA}大家都完成蜣螂历史啦！");
            else
            {
                desc.Add($"今天还有 {todayLack} 个人没打蜣螂历史");
                desc.Add($"本周一共{totalLackMember}个人少打共{totalLack}次蜣螂历史");


                //if (list.Count < 20)
                //{
                desc.Add("详情如下");
                desc.AddRange(list);
                desc.Add($"{include.emoji.SHOUZHI}\"提醒物种历史\" 立刻提醒");
                //}
                //else
                //{
                //    desc.Add($"{include.emoji.HAN}由于人数过多，请发送\"查询物种总览\"查看详情");
                //}
            }

            return string.Join("\n", desc);
        }
        /// <summary>
        /// 查询金鱼历史（文本）
        /// </summary>
        /// <param name="club"></param>
        /// <returns></returns>
        private string? QueryGroupWarHisotySpe2(Club club)
        {
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (weekday == 5) return "周五就不用查物种历史啦";

            int todayLack = 0;
            int totalLack = 0;
            int todayLackMemer = 0;
            int totalLackMember = 0;
            int todayBS = 0;
            int totalBS = 0;
            int todayBSMemer = 0;
            int totalBSMemer = 0;
            var list = new List<string>();

            foreach (var uid in club.Members)
            {
                var member = FindMember(uid);
                if (member is null) continue;

                // today_bs
                // bs
                // lack
                // today_lack
                var _list = new List<string>();

                var _todayLack = JSONHelper.ParseInt(member.Query("gw/today_lack"));
                var _totalLack = JSONHelper.ParseInt(member.Query("gw/lack"));
                var _todayBS = JSONHelper.ParseInt(member.Query("gw/today_bs"));
                var _totalBS = JSONHelper.ParseInt(member.Query("gw/bs"));

                if (_todayLack > 0)
                {
                    todayLack += _todayLack;
                    todayLackMemer++;
                    _list.Add($"今天少{_todayLack}次");

                }
                if (_totalLack > 0)
                {
                    totalLack += _totalLack;
                    totalLackMember++;
                    _list.Add($"共少{_totalLack}次");
                }
                if (_todayBS > 0)
                {
                    todayBS += _todayBS;
                    todayBSMemer++;
                    _list.Add($"今天鞭尸{_todayBS}次");
                }
                if (_totalBS > 0)
                {
                    totalBS += _totalBS;
                    totalBSMemer++;
                    _list.Add($"共鞭尸{_totalBS}次");
                }

                if (_list.Count > 0) list.Add($"{include.emoji.ZHUYI}[{member.NameOrUID()}]{string.Join("，", _list)}");
            }

            var desc = new List<string>
            {
                $"---- 金鱼历史 ----"
            };
            if (todayLack <= 0 && todayBS <= 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都完成金鱼历史啦！");
            }
            else
            {
                if (todayLackMemer > 0 || todayBSMemer > 0) desc.Add($"今天共{todayLackMemer}人共少打{todayLack}次，共{todayBSMemer}人共鞭尸{todayBS}次");
                if (totalLackMember > 0 || totalBSMemer > 0) desc.Add($"本周共{totalLackMember}人共少打{totalLack}次，共{totalBSMemer}人共鞭尸{totalBS}次");

                //if (list.Count < 10)
                //{
                desc.Add("详情如下");
                desc.AddRange(list);
                desc.Add($"{include.emoji.SHOUZHI}\"提醒物种历史\" 立刻提醒");
                //}
                //else
                //{
                //    desc.Add($"{include.emoji.HAN}由于人数过多，请发送\"查询物种总览\"查看详情");
                //}
            }
            desc.Add("");

            // 俱乐部战绩

            var clubData = club.GroupWarData?.dbase?.spe2_record ?? new JObject();
            if (JSONHelper.GetCount(clubData) <= 0) desc.Add("(战果记录空，每天22:00结算之后登录获取)");
            else
            {
                foreach (var item in clubData)
                {
                    var _weekday = JSONHelper.ParseInt(item.Name);
                    if (_weekday <= 0 || _weekday > 7) continue;
                    var data = item.Value;
                    desc.Add($"---- {include.misc.WeekdayDesc(_weekday)}战绩 ----");
                    desc.Add(JSONHelper.ParseString(data.result));
                    desc.Add(JSONHelper.ParseString(data.score));
                    desc.Add(JSONHelper.ParseString(data.group_score));
                }
            }

            return string.Join("\n", desc);
        }
        /// <summary>
        /// 查询北极贝历史（文本）
        /// </summary>
        /// <param name="club"></param>
        /// <returns></returns>
        private string? QueryGroupWarHisotySpe3(Club club)
        {
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";
            int late = 0;
            int notStarted = 0;
            int notUseToday = 0;
            int notFetchToday = 0;
            int notUseTodayMember = 0;
            int notFetchTodayMember = 0;
            int notUseTotal = 0;
            int notFetchTotaj = 0;
            int notUseTotalMember = 0;
            int notFetcTotalMember = 0;
            var list = new List<string>();

            foreach (var uid in club.Members)
            {
                var member = FindMember(uid);
                if (member is null) continue;

                var _list = new List<string>();

                var _notUseToday = JSONHelper.ParseInt(member.Query("gw/not_use_today"));
                var _notUseTotal = JSONHelper.ParseInt(member.Query("gw/not_use"));
                var _lackToday = JSONHelper.ParseInt(club.Spe3DontNeedBuyGem ? member.Query("gw/lack_today_not_buy") : member.Query("gw/lack_today"));
                var _lackTotal = JSONHelper.ParseInt(club.Spe3DontNeedBuyGem ? member.Query("gw/lack_not_buy") : member.Query("gw/lack"));
                var _isStarted = JSONHelper.ParseBool(member.Query("gw/is_started"));
                var _isLate = JSONHelper.ParseLong(member.Query("gw/is_late"));

                if (_lackToday > 0)
                {
                    notFetchToday += _lackToday;
                    notFetchTodayMember++;
                    _list.Add($"今天少领{_lackToday}卷轴");

                }
                if (_lackTotal > 0)
                {
                    notFetchTotaj += _lackTotal;
                    notFetcTotalMember++;
                    _list.Add($"共少领{_lackTotal}卷轴");
                }
                if (_notUseToday > 0)
                {
                    notUseToday += _notUseToday;
                    notUseTodayMember++;
                    _list.Add($"今天少用{_notUseToday}卷轴");
                }
                if (_notUseTotal > 0)
                {
                    notUseTotal += _notUseTotal;
                    notUseTotalMember++;
                    _list.Add($"共少用{_notUseTotal}卷轴");
                }
                if (!_isStarted)
                {
                    notStarted++;
                    _list.Add("今天未开始修塔");
                }
                if (_isLate > 9)
                {
                    late++;
                    _list.Add($"今天修塔晚了 {TimeHelper.ChinsesTimeDurationDesc(_isLate)}");
                }


                if (_list.Count > 0) list.Add($"{include.emoji.ZHUYI} [{member.NameOrUID()}]{string.Join("，", _list)}");
            }

            var desc = new List<string>
            {
                $"---- 北极贝历史 ----"
            };
            if (late <= 0 && notStarted <= 0 && notUseToday <= 0 && notFetchToday <= 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都完成北极贝历史啦！");
            }
            else
            {
                if (notStarted > 0) desc.Add($"今天共{notStarted}人还未开始修塔");
                if (late > 0) desc.Add($"今天共{late}人修塔开始太晚");
                if (notFetchTodayMember > 0 || notUseTodayMember > 0) desc.Add($"今天卷轴共{notFetchTodayMember}人共少领{notFetchToday}个，共{notUseTodayMember}人共少用{notUseToday}个");
                if (notFetcTotalMember > 0 || notUseTotalMember > 0) desc.Add($"本周卷轴共{notFetcTotalMember}人共少领{notFetchTotaj}个，共{notUseTotalMember}人共少用{notUseTotal}个");


                //if (list.Count < 10)
                //{
                desc.Add("详情如下");
                desc.AddRange(list);
                desc.Add($"{include.emoji.SHOUZHI}\"提醒物种历史\" 立刻提醒");
                //}
                //else
                //{
                //    desc.Add($"{include.emoji.HAN}由于人数过多，请发送\"查询物种总览\"查看详情");
                //}
            }

            var clubData = club.GroupWarData?.dbase;
            if (clubData is JObject && JSONHelper.GetCount(clubData) > 0)
            {
                desc.Add("---- 俱乐部信息 ----");
                desc.Add($"魔法塔修建高度：{JSONHelper.ParseInt(clubData?.tower_height)}米");
                desc.Add($"魔法塔总计高度：{JSONHelper.ParseInt(clubData?.total_height)}米");
                desc.Add($"俱乐部禁忌之影数量：{JSONHelper.ParseInt(clubData?.exchange_item_c)}");
                desc.Add($"俱乐部魔法能量：{JSONHelper.ParseInt(clubData?.mp):N0}");
                desc.Add($"护盾数量：{JSONHelper.ParseInt(clubData?.shield):N0}");
            }
            return string.Join("\n", desc);
        }
        /// <summary>
        /// 查询仓鼠历史（文本）
        /// </summary>
        /// <param name="club"></param>
        /// <returns></returns>
        private string? QueryGroupWarHisotySpe4(Club club)
        {
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (weekday == 5) return "周五就不用查物种历史啦";
            int dice = 0;
            int redDice = 0;
            int diceMembers = 0;
            int redDiceMembers = 0;
            int notAuction = 0;
            int notSupply = 0;
            var list = new List<string>();

            foreach (var uid in club.Members)
            {
                var member = FindMember(uid);
                if (member is null) continue;

                var _list = new List<string>();

                var _dice = JSONHelper.ParseInt(member.Query("gw/dice"));
                var _redDice = JSONHelper.ParseInt(member.Query("gw/red_dice"));
                var _notAuction = JSONHelper.ParseInt(member.Query("gw/lack_auction"));
                var _notSupply = JSONHelper.ParseInt(member.Query("gw/not_supply"));

                if (_dice > 0)
                {
                    dice += _dice;
                    diceMembers++;
                    _list.Add($"还剩{dice}个骰子");
                }

                if (weekday == 4 && _redDice > 0)
                {
                    redDice += _redDice;
                    redDiceMembers++;
                    _list.Add($"还剩{dice}个红骰子");

                }
                if ((!club.Spe4DontNeedAuction) && (weekday >= 7 || weekday <= 3) && _notAuction > 0)
                {
                    notAuction++;
                    _list.Add($"今天没参与拍卖");
                }
                if ((weekday >= 6 || weekday <= 3) && _notSupply > 0)
                {
                    notSupply++;
                    _list.Add($"今天没领补给");
                }

                if (_list.Count > 0) list.Add($"{include.emoji.ZHUYI} [{member.NameOrUID()}]{string.Join("，", _list)}");
            }

            var desc = new List<string>
            {
                $"---- 仓鼠历史 ----"
            };

            if (dice <= 0 && notAuction <= 0 && notSupply <= 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都完成仓鼠历史啦！");
            }
            else
            {
                if (dice > 0) desc.Add($"共{diceMembers}人还剩共{dice}个骰子");
                if (notAuction > 0) desc.Add($"今天共{notAuction}人未参与拍卖");
                if (notSupply > 0) desc.Add($"今天共{notSupply}人未领补给");

                //if (list.Count < 10)
                //{
                desc.Add("详情如下");
                desc.AddRange(list);
                desc.Add($"{include.emoji.SHOUZHI}\"提醒物种历史\" 立刻提醒");
                //}
                //else
                //{
                //    desc.Add($"{include.emoji.HAN}由于人数过多，请发送\"查询物种总览\"查看详情");
                //}
            }

            desc.Add("---- 俱乐部信息 ----");
            desc.Add($"{include.emoji.SHOUZHI}\"查询仓鼠拍卖总览\" 生成包含以下三个页面的Excel文件");
            desc.Add(" - 本周内拍卖结果记录 (需每天结算后登录获取)");
            desc.Add(" - 本周内拍卖参与俱乐部信息");
            desc.Add(" - 本周内拍卖地块信息");

            return string.Join("\n", desc);
        }
        /// <summary>
        /// 查询螳螂历史（文本）
        /// </summary>
        /// <param name="club"></param>
        /// <returns></returns>
        private string? QueryGroupWarHisotySpe5(Club club)
        {
            if (!club.IsTodayUpdated()) return "今天还没更新过物种数据，请先登录一次再来查询";
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            if (weekday == 5) return "周五就不用查物种历史啦";
            int lack = 0;
            var list = new List<string>();

            foreach (var uid in club.Members)
            {
                var member = FindMember(uid);
                if (member is null) continue;

                var _list = new List<string>();

                var _lack = JSONHelper.ParseInt(member.Query("gw/lack"));

                if (_lack > 0)
                {
                    lack++;
                    _list.Add($"未上擂台");

                }
                if (_list.Count > 0) list.Add($"{include.emoji.ZHUYI} [{member.NameOrUID()}]{string.Join("，", _list)}");
            }

            var desc = new List<string>
            {
                $"---- 螳螂历史 ----"
            };
            if (lack <= 0)
            {
                desc.Add($"{include.emoji.HAHA}大家都完成螳螂历史啦！");
            }
            else
            {
                desc.Add($"今天共{lack}人未上擂台");
            }

            if (list.Count > 0)
            {
                desc.Add("详情如下");
                desc.AddRange(list);
            }
            //else
            //{
            //    desc.Add($"{include.emoji.HAN}由于人数过多，请发送\"查询物种总览\"查看详情");
            //}

            return string.Join("\n", desc);
        }
        /// <summary>
        /// 生成 仓鼠历史拍卖总览 - 拍卖结果表
        /// </summary>
        /// <param name="club"></param>
        /// <returns></returns>
        private List<List<string>> GetGroupWarSpe4AuctionResultData(Club club)
        {
            var ret = new List<List<string>> {
                new List<string>
                {
                    "日期",
                    "拍卖已完成",
                    "参与俱乐部数量",
                    "拍卖地块数量",
                    "赢得地块数量",
                    "赢得地块",
                },
            };

            if (!club.IsTodayUpdated()) return ret;

            var data = club.GroupWarData?.dbase ?? new JObject();
            foreach (var dayItem in data)
            {
                var weekday = JSONHelper.ParseInt(dayItem.Name);
                if (weekday <= 0 || weekday > 7) continue;
                var result = dayItem.Value;
                result = result.result ?? new JObject();
                var grids = JSONHelper.ParseStringList(result.won_grids);
                ret.Add(new List<string>
                {
                    include.misc.WeekdayDesc(weekday),
                    JSONHelper.ParseBool(result.not_finished)?"否":"是",
                    JSONHelper.ParseInt(result.club_count).ToString(),
                    JSONHelper.ParseInt(result.grid_count).ToString(),
                    JSONHelper.ParseInt(result.won_count).ToString(),
                    string.Join("/", grids),

                });
            }
            return ret;
        }
        /// <summary>
        /// 生成 仓鼠历史拍卖总览 - 参与俱乐部表
        /// </summary>
        /// <param name="club"></param>
        /// <returns></returns>
        private List<List<string>> GetGroupWarSpe4AuctionClubsData(Club club)
        {
            var ret = new List<List<string>> {
                new List<string>
                {
                    "日期",
                    "RID",
                    "俱乐部名称",
                    "成员数量",
                    "参与拍卖人数",
                    "总战力",
                    "区服",
                },
            };

            if (!club.IsTodayUpdated()) return ret;

            var data = club.GroupWarData?.dbase ?? new JObject();
            foreach (var dayItem in data)
            {
                var weekday = JSONHelper.ParseInt(dayItem.Name);
                if (weekday <= 0 || weekday > 7) continue;
                var clubs = dayItem.Value;
                clubs = clubs.clubs ?? new JObject();
                foreach (var _club in clubs)
                {
                    var uid = _club.Name;
                    var clubData = _club.Value;
                    ret.Add(new List<string>
                    {
                        include.misc.WeekdayDesc(weekday),
                        uid,
                        JSONHelper.ParseString(clubData.name),
                        JSONHelper.ParseInt(clubData.member_amount).ToString(),
                        JSONHelper.ParseInt(clubData.auction).ToString(),
                        JSONHelper.ParseLong(clubData.total_score).ToString("N0"),
                        JSONHelper.ParseString(clubs.server),
                    });
                }

            }
            return ret;
        }
        /// <summary>
        /// 生成 仓鼠历史拍卖总览 - 地块表
        /// </summary>
        /// <param name="club"></param>
        /// <returns></returns>
        private List<List<string>> GetGroupWarSpe4AuctionBlocksData(Club club, bool isAdmin = false)
        {
            var ret = new List<List<string>> {
                new List<string>
                {
                    "日期",
                    "位置",
                    "名称",
                    "我方参与人数",
                    "总计参与人数",
                },
            };

            if (!club.IsTodayUpdated()) return ret;

            var data = club.GroupWarData?.dbase ?? new JObject();
            foreach (var dayItem in data)
            {
                var weekday = JSONHelper.ParseInt(dayItem.Name);
                if (weekday <= 0 || weekday > 7) continue;
                var blocks = dayItem.Value;
                blocks = blocks.blocks ?? new JObject();
                foreach (var _club in blocks)
                {
                    var pos = _club.Name;
                    var blockData = _club.Value;
                    var totalCount = JSONHelper.ParseInt(blockData.total_count_detail);
                    ret.Add(new List<string>
                    {
                        include.misc.WeekdayDesc(weekday),
                        pos,
                        JSONHelper.ParseString(blockData.name),
                        JSONHelper.ParseInt(blockData.my_count).ToString(),
                        isAdmin ? totalCount.ToString(): GetSpecial4RangeDesc(totalCount),
                    });
                }

            }
            return ret;
        }
        private string GetSpecial4RangeDesc(int num)
        {
            var str = "";
            if (num <= 50)
            {
                var ranges = new List<int> { 10, 30, 50 };
                int fixNum = 0;
                for (int i = 0; i <= 2; i++)
                {
                    if (num <= ranges[i])
                    {
                        fixNum = ranges[i];
                        str = $"<{fixNum}";
                        break;
                    }
                }
            }
            else
            {
                var fixNum = num - ((num - 1) % 50) - 1;
                str = $">{fixNum}";
            }
            return str;
        }
        /// <summary>
        /// 查询银令牌列表
        /// </summary>
        /// <param name="club"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private Dictionary<string, int> QueryGroupWarSilverTokenList(Club club, include.TokenQueryType type)
        {
            var ret = new Dictionary<string, int>();
            if (club is null || !club.IsTodayUpdated()) return ret;

            foreach (var uid in club.Members)
            {
                var member = FindMember(uid);
                if (member is null) continue;
                int count = 0;
                switch (type)
                {
                    case include.TokenQueryType.NOT_USE:
                        count = JSONHelper.ParseInt(member.Query("token_silver_not_use"));
                        break;
                    case include.TokenQueryType.LACK:
                        count = JSONHelper.ParseInt(member.Query("token_silver_lack_total")) * 100 +
                                JSONHelper.ParseInt(member.Query("token_silver_lack_today"));
                        break;
                    case include.TokenQueryType.BUY:
                        count = JSONHelper.ParseInt(member.Query("token_silver_buy"));
                        break;
                    case include.TokenQueryType.NOT_BUY:
                        count = JSONHelper.ParseInt(member.Query("token_silver_buy")) > 0 ? 0 : 1;
                        break;
                    default:
                        break;
                }
                if (count > 0) ret.Add(uid, count);
            }

            return ret;
        }
        /// <summary>
        /// 查询金令牌列表
        /// </summary>
        /// <param name="club"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private Dictionary<string, int> QueryGroupWarGoldTokenList(Club club, include.TokenQueryType type)
        {
            var ret = new Dictionary<string, int>();
            if (club is null || !club.IsTodayUpdated()) return ret;

            foreach (var uid in club.Members)
            {
                var member = FindMember(uid);
                if (member is null) continue;
                int count = 0;
                switch (type)
                {
                    case include.TokenQueryType.NOT_USE:
                        count = JSONHelper.ParseInt(member.Query("token_gold_not_use"));
                        break;
                    case include.TokenQueryType.LACK:
                        count = JSONHelper.ParseInt(member.Query("token_gold_lack_total")) * 100 +
                                JSONHelper.ParseInt(member.Query("token_gold_lack_today"));
                        break;
                    case include.TokenQueryType.BUY:
                        count = JSONHelper.ParseInt(member.Query("token_gold_buy"));
                        break;
                    case include.TokenQueryType.NOT_BUY:
                        count = JSONHelper.ParseInt(member.Query("token_gold_buy")) > 0 ? 0 : 1;
                        break;
                    default:
                        break;
                }
                if (count > 0) ret.Add(uid, count);
            }

            return ret;
        }
        /// <summary>
        /// 查询钻头列表
        /// </summary>
        /// <param name="club"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private Dictionary<string, int> QueryGroupWarDrillList(Club club, include.DrillQueryType type, int min = 0)
        {
            var ret = new Dictionary<string, int>();
            if (club is null || !club.IsTodayUpdated()) return ret;
            var sum = new Func<int, int>((int kit) => kit switch
            {
                1 or 3 => club.DrillUseLimit2,
                2 => club.DrillUseLimit1,
                _ => 0,
            });
            foreach (var uid in club.Members)
            {
                var member = FindMember(uid);
                if (member is null) continue;
                int count = 0;
                switch (type)
                {
                    case include.DrillQueryType.NOT_USE:
                        count = JSONHelper.ParseInt(member.Query("drill_total")) - JSONHelper.ParseInt(member.Query("drill"));
                        if (min > 0 && count < min) count = 0;
                        if (club.RemindDrillLowerLimit > 0 && count < club.RemindDrillLowerLimit) count = 0;
                        break;
                    case include.DrillQueryType.LACK:
                        count = JSONHelper.ParseInt(member.Query("drill"));
                        var drillStd = sum((int)member.Kit);
                        if (drillStd == 0) count = 0;
                        else count = Math.Max(0, drillStd - count);
                        break;
                    default:
                        break;
                }
                if (count > 0) ret.Add(uid, count);
            }

            return ret;
        }
        /// <summary>
        /// 查询参与事件次数
        /// </summary>
        /// <param name="club"></param>
        /// <param name="onlyZero"></param>
        /// <returns></returns>
        private Dictionary<string, int> QueryGroupWarMultiEventCountList(Club club, bool onlyZero = false)
        {
            var ret = new Dictionary<string, int>();
            if (club is null || !club.IsTodayUpdated()) return ret;

            foreach (var uid in club.Members)
            {
                var member = FindMember(uid);
                if (member is null) continue;
                int count = JSONHelper.ParseInt(member.Query("event_multi_count"));

                if (onlyZero && count > 0) continue;
                ret.Add(uid, count);
            }
            return ret;
        }
        /// <summary>
        /// 查询挖矿到期时间
        /// </summary>
        /// <param name="club"></param>
        /// <returns></returns>
        private Dictionary<string, long> QueryGroupWarMineDestList(Club club)
        {
            var ret = new Dictionary<string, long>();
            if (club is null || !club.IsTodayUpdated()) return ret;

            foreach (var uid in club.Members)
            {
                var member = FindMember(uid);
                if (member is null) continue;
                var dest = JSONHelper.ParseLong(member.Query("mine_dest"));
                ret.Add(uid, dest);
            }

            return ret;
        }
        /// <summary>
        /// 查询处于同一矿内的UID列表
        /// </summary>
        /// <param name="club"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private List<string> QueryAtSameMinePos(Club club, string pos)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(pos)) return list;
            foreach (var uid in club.Members)
            {
                var member = FindMember(uid);
                if (member is null) continue;
                var position = JSONHelper.ParseString(member.Query("mine_pos"));
                if (position == pos) list.Add(uid);
            }

            return list;
        }
        /// <summary>
        /// 查询挖矿提醒内容
        /// </summary>
        /// <param name="club"></param>
        /// <param name="near"></param>
        /// <returns></returns>
        private Dictionary<string, List<string>> QueryGroupWarMineRemind(Club club, double near = 0.5)
        {
            var ret = new Dictionary<string, List<string>>();
            if (club is null || !club.IsTodayUpdated()) return ret;
            var nearSec = Convert.ToInt64(near * 3600);
            var now = TimeHelper.ToTimeStamp();
            var hour = DateTime.Now.Hour;

            foreach (var item in QueryGroupWarMineDestList(club))
            {

                if (item.Value <= 1000000000)
                {
                    if (!ret.ContainsKey(item.Key)) ret.Add(item.Key, new());
                    if (club.RemindContentNotCombine) ret[item.Key].Add($"{include.emoji.YIWEN}跳矿或离矿");
                    else ret[item.Key].Add($"{include.emoji.YIWEN}跳矿或离矿");
                }
                else if (item.Value <= now)
                {
                    if (!ret.ContainsKey(item.Key)) ret.Add(item.Key, new());
                    if (club.RemindContentNotCombine) ret[item.Key].Add($"{include.emoji.BAOZHA}矿已超时{TimeHelper.ChinsesTimeDurationDesc(now - item.Value)}");
                    else ret[item.Key].Add($"{include.emoji.BAOZHA}挖矿超时");
                }
                else if (item.Value <= now + nearSec)
                {
                    if (club.DontRemindMineClose) continue;
                    if (!ret.ContainsKey(item.Key)) ret.Add(item.Key, new());
                    if (club.RemindContentNotCombine) ret[item.Key].Add($"{include.emoji.NAOZHONG}矿将在{TimeHelper.ChinsesTimeDurationDesc(item.Value - now)}后挖完");
                    else ret[item.Key].Add($"{include.emoji.NAOZHONG}挖矿即将超时");
                }
                else if (hour >= 21 && item.Value <= now + 6 * 3600)
                {
                    if (club.DontRemindMineClose) continue;
                    if (!ret.ContainsKey(item.Key)) ret.Add(item.Key, new());
                    if (club.RemindContentNotCombine) ret[item.Key].Add($"{include.emoji.NAOZHONG}矿将在{TimeHelper.ChinsesTimeDurationDesc(item.Value - now)}后挖完");
                    else ret[item.Key].Add($"{include.emoji.NAOZHONG}睡觉前换矿");
                }
            }

            return ret;
        }
        /// <summary>
        /// 查询金牌提醒内容
        /// </summary>
        /// <param name="club"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private Dictionary<string, List<string>> QueryGroupWarTokenGoldRemind(Club club, TokenQueryType type)
        {
            var ret = new Dictionary<string, List<string>>();
            if (club is null || !club.IsTodayUpdated()) return ret;

            foreach (var item in QueryGroupWarGoldTokenList(club, type))
            {
                if (!ret.ContainsKey(item.Key)) ret.Add(item.Key, new());
                switch (type)
                {
                    case include.TokenQueryType.NOT_USE:
                        if (club.RemindContentNotCombine) ret[item.Key].Add($"{include.emoji.GOLD}金牌还剩{item.Value}");
                        else ret[item.Key].Add($"{include.emoji.GOLD}金牌没用完");
                        break;
                    case include.TokenQueryType.LACK:
                        if (club.RemindContentNotCombine) ret[item.Key].Add($"{include.emoji.GOLD}金牌少领{item.Value % 100}");
                        else ret[item.Key].Add($"{include.emoji.GOLD}金牌没领");
                        break;
                    case include.TokenQueryType.NOT_BUY:
                        if (club.RemindContentNotCombine) ret[item.Key].Add($"{include.emoji.GOLD}金牌没氪");
                        else ret[item.Key].Add($"{include.emoji.GOLD}金牌未氪");
                        break;
                    default:
                        break;
                }
            }

            return ret;
        }
        /// <summary>
        /// 查询银牌提醒内容
        /// </summary>
        /// <param name="club"></param>
        /// <param name="type"></param>
        /// <returns>{{uid,{desc1, ...}}}</returns>
        private Dictionary<string, List<string>> QueryGroupWarTokenSilverRemind(Club club, TokenQueryType type)
        {
            var ret = new Dictionary<string, List<string>>();
            if (club is null || !club.IsTodayUpdated()) return ret;

            foreach (var item in QueryGroupWarSilverTokenList(club, type))
            {
                if (!ret.ContainsKey(item.Key)) ret.Add(item.Key, new());
                switch (type)
                {
                    case include.TokenQueryType.NOT_USE:
                        if (club.RemindContentNotCombine) ret[item.Key].Add($"{include.emoji.SILVER}银牌还剩{item.Value}");
                        else ret[item.Key].Add($"{include.emoji.SILVER}银牌没用完");
                        break;
                    case include.TokenQueryType.LACK:
                        if (club.RemindContentNotCombine) ret[item.Key].Add($"{include.emoji.SILVER}银牌少领{item.Value % 100}");
                        else ret[item.Key].Add($"{include.emoji.SILVER}银牌没领");
                        break;
                    case include.TokenQueryType.NOT_BUY:
                        if (club.RemindContentNotCombine) ret[item.Key].Add($"{include.emoji.SILVER}银牌没氪");
                        else ret[item.Key].Add($"{include.emoji.SILVER}银牌未氪");
                        break;
                    default:
                        break;
                }
            }

            return ret;
        }
        /// <summary>
        /// 查询钻头提醒内容
        /// </summary>
        /// <param name="club"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private Dictionary<string, List<string>> QueryGroupWarDrillRemind(Club club, DrillQueryType type, int min = 0, string notice = "")
        {
            var ret = new Dictionary<string, List<string>>();
            if (club is null || !club.IsTodayUpdated()) return ret;
            var desc = "";
            if (type == DrillQueryType.NOT_USE)
            {
                if (string.IsNullOrEmpty(notice)) desc += "";
                else desc = $"，请尽快{notice}";
            }

            foreach (var item in QueryGroupWarDrillList(club, type, min))
            {
                if (!ret.ContainsKey(item.Key)) ret.Add(item.Key, new());
                switch (type)
                {
                    case include.DrillQueryType.NOT_USE:
                        if (club.RemindContentNotCombine) ret[item.Key].Add($"{include.emoji.DRILL}钻头还剩{item.Value}{desc}");
                        else ret[item.Key].Add($"{include.emoji.DRILL}钻头没用完{desc}");
                        break;
                    case include.DrillQueryType.LACK:
                        if (club.RemindContentNotCombine) ret[item.Key].Add($"{include.emoji.DRILL}钻头还差{item.Value}");
                        else ret[item.Key].Add($"{include.emoji.DRILL}钻头没达标");
                        break;
                    default:
                        break;
                }
            }
            return ret;
        }
        /// <summary>
        /// 查询物种历史提醒内容
        /// </summary>
        /// <param name="club"></param>
        /// <returns></returns>
        private Dictionary<string, List<string>> QueryGroupWarHistoryRemind(Club club, bool isAuto = false)
        {
            if (club is null || !club.IsTodayUpdated()) return new Dictionary<string, List<string>>();

            switch (club.Map)
            {
                case "1": // 蜣螂历史
                    return QueryGroupWarHisotySpe1Remind(club, isAuto);
                case "2": // 金鱼历史
                    return QueryGroupWarHisotySpe2Remind(club, isAuto);
                case "3": // 北极贝历史
                    return QueryGroupWarHisotySpe3Remind(club, isAuto);
                case "4": // 仓鼠历史
                    return QueryGroupWarHisotySpe4Remind(club, isAuto);
                case "5": // 螳螂历史
                    return QueryGroupWarHisotySpe5Remind(club, isAuto);
            }

            return new Dictionary<string, List<string>>();
        }
        /// <summary>
        /// 查询蜣螂历史提醒内容
        /// </summary>
        /// <param name="club"></param>
        /// <returns></returns>
        private Dictionary<string, List<string>> QueryGroupWarHisotySpe1Remind(Club club, bool isAuto = false)
        {
            var list = new List<string>();
            var ret = new Dictionary<string, List<string>>();

            // 每天17点（含17点）之后，提醒当天缺勤成员，手动提醒无时间限制。
            if (isAuto)
            {
                var hour = DateTime.Now.Hour;
                if (hour < 17) return ret;
            }

            foreach (var uid in club.Members)
            {
                var member = FindMember(uid);
                if (member is null) continue;

                if (JSONHelper.ParseInt(member.Query("gw/today_count")) < 1)
                {
                    if (!ret.ContainsKey(uid)) ret.Add(uid, new());
                    if (club.RemindContentNotCombine) ret[uid].Add($"{include.emoji.SPE1}蜣螂没打");
                    else ret[uid].Add($"{include.emoji.SPE1}蜣螂没打");
                }
            }
            return ret;
        }
        /// <summary>
        /// 查询金鱼历史提醒内容
        /// </summary>
        /// <param name="club"></param>
        /// <returns></returns>
        private Dictionary<string, List<string>> QueryGroupWarHisotySpe2Remind(Club club, bool isAuto = false)
        {
            var list = new List<string>();
            var ret = new Dictionary<string, List<string>>();

            // 每天17点到22点之间，提醒当天攻击少于2次的成员，手动提醒22点之前有效。
            var hour = DateTime.Now.Hour;
            if (isAuto && hour < 17) return ret;
            if (hour >= 22) return ret;

            foreach (var uid in club.Members)
            {
                var member = FindMember(uid);
                if (member is null) continue;

                var _todayLack = JSONHelper.ParseInt(member.Query("gw/today_lack"));

                if (_todayLack > 0)
                {
                    if (!ret.ContainsKey(uid)) ret.Add(uid, new());
                    if (club.RemindContentNotCombine) ret[uid].Add($"{include.emoji.SPE2}金鱼少打{_todayLack}次");
                    else ret[uid].Add($"{include.emoji.SPE2}金鱼没打");
                }
            }
            return ret;
        }
        /// <summary>
        /// 查询北极贝历史提醒内容
        /// </summary>
        /// <param name="club"></param>
        /// <returns></returns>
        private Dictionary<string, List<string>> QueryGroupWarHisotySpe3Remind(Club club, bool isAuto = false)
        {
            var list = new List<string>();
            var ret = new Dictionary<string, List<string>>();

            // 每天12点（含12点）之后，提醒未开始修塔的成员，每天17点（含17点）之后，提醒未领完和使用完卷轴的成员，手动提醒无时间限制。
            var hour = DateTime.Now.Hour;


            foreach (var uid in club.Members)
            {
                var member = FindMember(uid);
                if (member is null) continue;

                var _notUseToday = JSONHelper.ParseInt(member.Query("gw/not_use_today"));
                var _lackToday = JSONHelper.ParseInt(club.Spe3DontNeedBuyGem ? member.Query("gw/lack_today_not_buy") : member.Query("gw/lack_today"));
                var _isStarted = JSONHelper.ParseBool(member.Query("gw/is_started"));

                if (_notUseToday > 0 && ((isAuto && hour >= 17) || !isAuto))
                {
                    if (!ret.ContainsKey(uid)) ret.Add(uid, new());
                    if (club.RemindContentNotCombine) ret[uid].Add($"{include.emoji.SPE3}北极贝卷轴剩{_notUseToday}个");
                    else ret[uid].Add($"{include.emoji.SPE3}北极贝卷轴没用完");
                }

                if (_lackToday > 0 && ((isAuto && hour >= 17) || !isAuto))
                {
                    if (!ret.ContainsKey(uid)) ret.Add(uid, new());
                    if (club.RemindContentNotCombine) ret[uid].Add($"{include.emoji.SPE3}北极贝卷轴少{_lackToday}个");
                    else ret[uid].Add($"{include.emoji.SPE3}北极贝卷轴没领完");
                }

                if (!_isStarted && ((isAuto && hour >= 12) || !isAuto))
                {
                    if (!ret.ContainsKey(uid)) ret.Add(uid, new());
                    if (club.RemindContentNotCombine) ret[uid].Add($"{include.emoji.SPE3}没开始修塔");
                    else ret[uid].Add($"{include.emoji.SPE3}没开始修塔");
                }
            }
            return ret;
        }
        /// <summary>
        /// 查询仓鼠历史提醒内容
        /// </summary>
        /// <param name="club"></param>
        /// <returns></returns>
        private Dictionary<string, List<string>> QueryGroupWarHisotySpe4Remind(Club club, bool isAuto = false)
        {
            var list = new List<string>();
            var ret = new Dictionary<string, List<string>>();

            // 每周日/一/二/三20点（含20点）之后，提醒未参与拍卖的成员
            // 每周六/日/一/二/三17点（含17点）之后，提醒未领取补给的成员
            // 每周四20点（含20点）之后，提醒未用完骰子和红骰子的成员
            // 手动提醒仅受日期限制，不受当日时间限制
            var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
            var hour = DateTime.Now.Hour;

            foreach (var uid in club.Members)
            {
                var member = FindMember(uid);
                if (member is null) continue;

                var _dice = JSONHelper.ParseInt(member.Query("gw/dice"));
                var _redDice = JSONHelper.ParseInt(member.Query("gw/red_dice"));
                var _notAuction = JSONHelper.ParseInt(member.Query("gw/lack_auction"));
                var _notSupply = JSONHelper.ParseInt(member.Query("gw/not_supply"));

                if (_dice > 0 && weekday == 4 && (!isAuto || hour >= 20))
                {
                    if (!ret.ContainsKey(uid)) ret.Add(uid, new());
                    if (club.RemindContentNotCombine) ret[uid].Add($"{include.emoji.SPE4}仓鼠骰子剩{_dice}个");
                    else ret[uid].Add($"{include.emoji.SPE4}仓鼠骰子没用完");
                }

                if ((!club.Spe4DontNeedAuction) && _notAuction > 0 && (weekday >= 7 || weekday <= 3) && (!isAuto || hour >= 20))
                {
                    if (!ret.ContainsKey(uid)) ret.Add(uid, new());
                    if (club.RemindContentNotCombine) ret[uid].Add($"{include.emoji.SPE4}仓鼠没参加拍卖");
                    else ret[uid].Add($"{include.emoji.SPE4}仓鼠没参加拍卖");
                }

                if (_notSupply > 0 && (weekday >= 6 || weekday <= 3) && (!isAuto || hour >= 17))
                {
                    if (!ret.ContainsKey(uid)) ret.Add(uid, new());
                    if (club.RemindContentNotCombine) ret[uid].Add($"{include.emoji.SPE4}仓鼠没领补给");
                    else ret[uid].Add($"{include.emoji.SPE4}仓鼠没领补给");
                }
            }
            return ret;
        }
        /// <summary>
        /// 查询螳螂历史提醒内容
        /// </summary>
        /// <param name="club"></param>
        /// <returns></returns>
        private Dictionary<string, List<string>> QueryGroupWarHisotySpe5Remind(Club club, bool isAuto = false)
        {
            var list = new List<string>();
            var ret = new Dictionary<string, List<string>>();

            // 每周六/一/二/三/四12点至21点之间，提醒未上擂台的成员。手动提醒在当日21点前均有效。
            var hour = DateTime.Now.Hour;
            if (isAuto && hour < 12) return ret;
            if (hour >= 21) return ret;

            foreach (var uid in club.Members)
            {
                var member = FindMember(uid);
                if (member is null) continue;

                var _lack = JSONHelper.ParseInt(member.Query("gw/lack"));

                if (_lack > 0)
                {
                    if (!ret.ContainsKey(uid)) ret.Add(uid, new());
                    if (club.RemindContentNotCombine) ret[uid].Add($"{include.emoji.SPE5}螳螂没上擂台");
                    else ret[uid].Add($"{include.emoji.SPE5}螳螂没上擂台");
                }
            }
            return ret;
        }
        /// <summary>
        /// 合并提醒查询数据
        /// </summary>
        /// <param name="tarDic"></param>
        /// <param name="srcDic"></param>
        /// <param name="combineExcludedDesc">当字典中含有此词条时不再继续添加词条</param>
        /// <returns></returns>
        private Dictionary<string, List<string>> CombineRemindDic(Dictionary<string, List<string>> tarDic,
                                                                  Dictionary<string, List<string>> srcDic,
                                                                  string combineExcludedDesc = "")
        {
            var ret = tarDic.DeepCopy();
            var silverTag = $"{include.emoji.SILVER}银牌没领";
            var goldTag = $"{include.emoji.GOLD}金牌没领";
            // {uid, {desc1, desc2, ...}}
            foreach (var item in srcDic)
            {
                if (!ret.ContainsKey(item.Key)) ret.Add(item.Key, new());
                foreach (var _item in item.Value)
                {
                    if (ret[item.Key].Contains(combineExcludedDesc)) continue;
                    if ((ret[item.Key].Contains(silverTag) && _item == goldTag) || (ret[item.Key].Contains(goldTag) && _item == silverTag))
                    {
                        ret[item.Key].Add($"{include.emoji.GOLD}{include.emoji.SILVER}金银牌没领");
                        ret[item.Key].Remove(silverTag);
                        ret[item.Key].Remove(goldTag);
                    }
                    else if (!ret[item.Key].Contains(_item)) ret[item.Key].Add(_item);
                }
            }
            return ret;
        }
        /// <summary>
        /// 处理并发出提醒
        /// </summary>
        /// <param name="club"></param>
        /// <param name=""></param>
        /// <returns></returns>
        private bool SendRemindContent(Club club, Dictionary<string, List<string>> data, string chatroom, string sender, bool isAuto = false)
        {
            try
            {
                var sendCount = 0;
                var notSendCount = 0;

                if (club.RemindContentNotCombine)
                {
                    // 不合并发出内容
                    // 发送失败列表（未绑定成员）
                    var notSend = new List<string>();
                    // 发送内容列表
                    var sendContent = new Dictionary<string, string>();
                    // 游戏UID与WXID字典
                    var uidWxid = new Dictionary<string, string>();
                    foreach (var item in data)
                    {
                        uidWxid[item.Key] = QueryMemberWxid(club.RID, item.Key) ?? "";
                    }

                    foreach (var item in data)
                    {
                        var wxid = uidWxid[item.Key];
                        if (string.IsNullOrEmpty(wxid))
                        {
                            notSendCount++;
                            notSend.Add($"[{QueryMemberName(item.Key)}]{string.Join("，", item.Value)}");
                        }
                        else
                        {
                            sendCount++;
                            if (!sendContent.ContainsKey(wxid)) sendContent[wxid] = "";
                            sendContent[wxid] += $"\n{include.emoji.ZHUYI}[{QueryMemberName(item.Key)}]{string.Join("，", item.Value)} ({sendCount})";
                        }
                    }
                    foreach (var item in sendContent)
                    {
                        _context.WechatM.SendAtText(item.Value, new() { item.Key }, chatroom, true);
                        //Console.WriteLine($"提醒：{item.Value}");
                    }

                    var notFound = new List<string>();
                    notFound.Add($"共找到{data.Count}人");
                    if (sendCount > 0) notFound.Add($"已提醒{sendCount}人");
                    if (notSendCount > 0) notFound.Add($"未提醒{notSendCount}人");
                    var notSendDesc = $"{string.Join("，", notFound)}:\n{string.Join("\n", notSend)}";
                    if (notSendDesc.Length > 200)
                    {
                        var fileName = $"OUT\\未发送成功的清资源提醒_@{_context.ContactsM.QueryGroupMemberNickForFile(sender, chatroom)}_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.txt";
                        fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                        System.IO.File.WriteAllText(fileName, notSendDesc);
                        _context.WechatM.SendFile(fileName, chatroom);
                    }
                    else
                    {
                        if (club.LoginDontAtHolder) _context.WechatM.SendText(notSendDesc, sender);
                        else _context.WechatM.SendAtText(notSendDesc,
                                                       new List<string> { sender },
                                                       chatroom,
                                                       true);
                    }
                }
                else
                {
                    // 合并发出内容 
                    // 生成一个字典，Key为wxid，Value为绑定的UID列表
                    var wxidUIDs = new Dictionary<string, List<string>>();
                    var uidWxid = new Dictionary<string, string>();
                    var group = _context.ContactsM.FindGroupByRID(club.RID);
                    foreach (var item in group.Members)
                    {
                        wxidUIDs[item.Key] = item.Value.UIDs?.ToList() ?? new List<string>();
                        foreach (var _item in wxidUIDs[item.Key])
                        {
                            uidWxid[_item] = item.Key;
                        }
                    }

                    var notSend = new List<string>();
                    //                              content,          wxid,      uid
                    var sendWxids = new Dictionary<string, Dictionary<string, List<string>>>();
                    // 将提醒列表转换为 内容-wxid-uids形式
                    foreach (var item in data)
                    {
                        var wxid = uidWxid.ContainsKey(item.Key) ? uidWxid[item.Key] : "";
                        if (string.IsNullOrEmpty(wxid))
                        {
                            notSendCount++;
                            notSend.Add($"[{QueryMemberName(item.Key)}]{string.Join("，", item.Value)}");
                        }
                        else
                        {
                            foreach (var desc in item.Value)
                            {
                                sendCount++;
                                if (!sendWxids.ContainsKey(desc)) sendWxids.Add(desc, new());
                                if (!sendWxids[desc].ContainsKey(wxid)) sendWxids[desc].Add(wxid, new());
                                sendWxids[desc][wxid].Add(item.Key);
                            }
                        }
                    }

                    foreach (var item in sendWxids)
                    {
                        var content = new List<List<string>> { new List<string>() };
                        var subContent = "";
                        var ats = new List<List<string>> { new List<string>() };
                        foreach (var member in item.Value)
                        {
                            var wxid = member.Key;
                            if (!ats.Last().Contains(wxid)) ats.Last().Add(wxid);
                            var oriCount = wxidUIDs[wxid].Count;
                            var curCount = member.Value.Count;
                            var wxNick = _context.ContactsM.QueryGroupMemberNick(wxid, group.WXID);
                            //if (oriCount > 1)
                            //{
                            //    if (oriCount == curCount)
                            //    {

                            //        // 一个人的所有角色提示
                            //        subContent = $"@{wxNick}{chars.AtSpliter}(所有角色)";
                            //    }
                            //    else
                            //    {
                            var nicks = new List<string>();
                            foreach (var uid in member.Value)
                            {
                                // 一个人的非所有角色提示
                                nicks.Add($"{QueryMemberName(uid)}");
                            }
                            subContent = $"@{wxNick}{chars.AtSpliter} [{string.Join("，", nicks)}]";
                            //    }
                            //}
                            //else
                            //{
                            //    // 一个人仅有一个角色的提示
                            //    subContent = $"@{wxNick}{chars.AtSpliter}";
                            //}
                            if ((string.Join("\n", content.Last()).Length + subContent.Length) > 200)
                            {
                                content.Add(new());
                                ats.Add(new());
                            }
                            content.Last().Add(subContent);
                        }

                        for (int i = 0; i < content.Count; i++)
                        {
                            var indexDesc = content.Count > 1 ? $"({i + 1}/{content.Count})" : "";
                            _context.WechatM.SendAtText($"{include.emoji.ZHUYI}{item.Key}{indexDesc}：\n{string.Join("\n", content[i])}",
                                                         ats[i],
                                                         chatroom,
                                                         false);
                            //Console.WriteLine($"提醒：{include.emoji.ZHUYI}{item.Key}{indexDesc}：\n{string.Join("，", content[i])}");
                        }

                    }
                    var notFound = new List<string>();
                    if (isAuto) notFound.Add($"[{club.Name}]登录完成，正在自动发出资源提醒");
                    notFound.Add($"共找到{data.Count()}人");
                    if (sendCount > 0) notFound.Add($"已提醒{data.Count() - notSendCount}人");
                    if (notSendCount > 0) notFound.Add($"未提醒{notSendCount}人");
                    var notSendDesc = $"{string.Join("，", notFound)}:\n{string.Join("\n", notSend)}";
                    if (notSendDesc.Length > 200)
                    {
                        var fileName = $"OUT\\未发送成功的清资源提醒_@{_context.ContactsM.QueryGroupMemberNickForFile(sender, chatroom)}_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.txt";
                        fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                        System.IO.File.WriteAllText(fileName, notSendDesc);
                        _context.WechatM.SendFile(fileName, chatroom);
                    }
                    else
                    {
                        if (club.LoginDontAtHolder) _context.WechatM.SendText($"{string.Join("，", notFound)}:\n{string.Join("\n", notSend)}", chatroom);
                        else _context.WechatM.SendAtText($"{string.Join("，", notFound)}:\n{string.Join("\n", notSend)}",
                                                     new List<string> { sender },
                                                     chatroom,
                                                     true);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "ClubsM.SendRemindContent");
                return false;
            }
        }
        /// <summary>
        /// 俱乐部允许发出最大提醒数量
        /// </summary>
        /// <param name="club"></param>
        /// <returns></returns>
        private int ClubRemidMaxCount(Club club) => club.RemindContentNotCombine ? 30 : 45;
        /// <summary>
        /// 为一个俱乐部分配套装
        /// </summary>
        /// <param name="club"></param>
        /// <param name="force">强制重新生成</param>
        private void CalcKitPlan(Club club, bool force = false)
        {
            if (club is null) return;
            if (!force)
            {
                var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
                // 每周五可以直接重新分配，其他时间判断本周内是否排过，若未排过则可以排一次
                if (weekday != 5 && club.IsWeekKitPlanned()) return;
            }

            var members = club.Members;
            var memberData = new Dictionary<string, int>();
            var memberInstances = new Dictionary<string, ClubMember>();
            int gansiCount = 0, kantanCount = 0;

            foreach (var uid in members)
            {
                var user = FindMember(uid);
                memberInstances[uid] = user;
                if (user is null) memberData[uid] = 0;
                else
                {
                    var lockedKit = user.KitLocked;
                    if (lockedKit == Kit.UNDEFINED)
                    {
                        var kitType = (club.ClubKitPlanType == ClubKitPlanType.UNDEFINED ? ClubKitPlanType.SONCOMBAT : club.ClubKitPlanType);
                        // 按兵种战力/实力/领导力进行排序和分配
                        if (kitType == ClubKitPlanType.FIVE)
                        {
                            int five = 0;
                            foreach (var attrib in include.club.FiveAttribs)
                            {
                                five += JSONHelper.ParseInt(user.Query(attrib));
                            }
                            memberData[uid] = five;
                        }
                        else memberData[uid] = JSONHelper.ParseInt(user.Query(include.club.ClubKitPlanTypeAttrib(kitType)));
                        //club.ClubKitPlanType switch
                        //{
                        //    ClubKitPlanType.COMBAT => JSONHelper.ParseInt(user.Query("combat")),
                        //    ClubKitPlanType.LEADERSHIP => JSONHelper.ParseInt(user.Query("leadership")),
                        //    ClubKitPlanType.SONCOMBAT => JSONHelper.ParseInt(user.Query("son_combat")),
                        //    _ => 0,
                        //};
                    }
                    else
                    {
                        // 若存在锁定套装设置
                        switch (lockedKit)
                        {
                            case Kit.GANSI:
                                gansiCount++;
                                break;
                            case Kit.KANTAN:
                                kantanCount++;
                                break;
                        }
                        user.KitAlloc = lockedKit;
                        user.Set("kit_index", 25);
                    }

                }
            }
            members = members.Where(a => memberData.ContainsKey(a)).ToList();
            members.Sort((a, b) => memberData[b] - memberData[a]);
            int index = 100;

            for (int i = 0; i < members.Count; i++)
            {
                Kit kit;
                if (gansiCount < 25)
                {
                    kit = Kit.GANSI;
                    gansiCount++;
                }
                else if (kantanCount < 25)
                {
                    kit = Kit.KANTAN;
                    kantanCount++;
                }
                else
                {
                    kit = Kit.KULI;
                }
                if (memberInstances.ContainsKey(members[i]) && memberInstances[members[i]] is not null)
                {
                    string curUid = members[i];
                    memberInstances[curUid].KitAlloc = kit;
                    memberInstances[curUid].Set("kit_index", index);
                    memberInstances[curUid].Set("kit_value", memberData[curUid]);
                    index--;
                }
            }

            memberInstances.Clear();
        }
        #endregion
    }
}
