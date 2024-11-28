using Google.Protobuf.WellKnownTypes;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.game.module;
using RS.Snail.JJJ.clone;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.modules
{
    /// <summary>
    /// 蜗牛客户端模组
    /// </summary>
    internal class SnailsM : IModule
    {
        #region FIELDS
        public string ModuleName { get => "SnailsM"; }
        public Context _context { get; set; }

        private bool _inited = false;
        public bool Inited { get => _inited; }

        private RS.Snail.JJJ.Client.Client _client;
        public RS.Snail.JJJ.Client.core.boot.Context ClientContext => _client.Context;
        #endregion

        #region INIT
        public SnailsM(boot.Context context)
        {
            this._context = context;
        }
        public void Init(bool load = false)
        {
            if (_inited) return;
            _inited = true;

            _client = new Client.Client(JSONHelper.ParseInt(_context.ConfigsM.QueryCommon("snail_client_capacity")))
            {
                OnLoginStart = OnLoginStart,
                OnLoginComplete = OnLoginCompleted,
                OnLoginFail = OnLoginFailed,
            };

            RegistSchedules();
        }

        private void RegistSchedules()
        {
            foreach (var rid in _context.ClubsM.GetLoginRIDsWithConfigSheet())
            {
                RegistOneClubAutoLoginSchedule(rid);
            }
            // 未配置自动登录配置表的俱乐部，跟随固定登录时间自动登录
            _context.ScheduleM.RegistSession("login_common",
                                            times: new List<string>
                                            {
                                                "6-12-30-0",
                                                "6-18-0-0",
                                                "6-20-0-0",

                                                "7-12-30-0",
                                                "7-18-0-0",
                                                "7-20-0-0",

                                                "1-12-30-0",
                                                "1-18-0-0",
                                                "1-20-0-0",

                                                "2-12-30-0",
                                                "2-18-0-0",
                                                "2-20-0-0",

                                                "3-12-30-0",
                                                "3-18-0-0",
                                                "3-20-0-0",

                                                "4-12-30-0",
                                                "4-18-0-0",
                                                "4-22-0-0",
                                            },
                                            action: new Action<string>(_ =>
                                            {
                                                StartLoginAllAutoWithoutSheet();
                                            }),
                                            isAutoLogin: true);

            _context.ScheduleM.RegistSession(tag: ModuleName,
                                             interval: 10,
                                             action: _client.CheckLoginTimeout,
                                             isAutoLogin: false);
        }


        // TODO: 
        // 登录重启继续
        public void RegistOneClubAutoLoginSchedule(string rid)
        {
            var club = _context.ClubsM.FindClub(rid);
            var tag = $"login_{rid}";
            if (club is null) return;
            _context.ScheduleM.UnregistSession(tag);
            _context.ScheduleM.RegistSession(tag,
                                             times: club.AllAutoLoginTimeTag(),
                                             action: new Action<string>(timeTag =>
                                             {
                                                 if (!club.IsPurchased()) return;
                                                 if (!club.LoginAuto) return;
                                                 StartLogin(rid,
                                                            isJustAttribs: false,
                                                            isAdministrator: false,
                                                            scheduleTimeTag: timeTag,
                                                            isAuto: true);
                                             }),
                                             isAutoLogin: true
                                             );
        }
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// 启动登录一个机器人下的所有账号
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, (bool result, string? desc)> StartLoginAllAutoWithoutSheet()
        {
            var ret = new Dictionary<string, (bool result, string? desc)>();
            var rids = _context.ClubsM.GetLoginRIDsWithoutConfigSheet(true);
            foreach (var rid in rids)
            {
                ret.Add(rid, StartLogin(rid, isJustAttribs: false, isAdministrator: false, isAuto: true));
            }
            return ret;
        }
        public Dictionary<string, (bool result, string? desc)> StartLoginAllManual(bool needRemind = false, bool needAttribs = false, bool isAdministrator = false)
        {
            var ret = new Dictionary<string, (bool result, string? desc)>();
            var rids = _context.ClubsM.GetLoginRIDsWithoutConfigSheet(false);
            foreach (var rid in rids)
            {
                ret.Add(rid, StartLogin(rid: rid, isJustAttribs: needAttribs, isAdministrator: isAdministrator, isAuto: false, needRemind: needRemind));
            }
            return ret;
        }
        /// <summary>
        /// 请求登录
        /// </summary>
        /// <param name="rid">俱乐部rid</param>
        /// <param name="isJustAttribs">是否仅获取演练九维</param>
        /// <param name="isAdministrator">是否超管权限</param>
        /// <returns></returns>
        public (bool result, string? desc) StartLoginOld(string rid, bool isJustAttribs = false, bool isAdministrator = false, bool isAuto = false)
        {
            if (_context.ConfigsM.SwitchLoginClose)
            {
                return (false, "当前已临时禁用登录功能");
            }

            var club = _context.ClubsM.FindClub(rid);
            if (club is null) return (false, "未找到俱乐部");
            var ap = club.GetRandomAccountPassword();
            if (string.IsNullOrEmpty(ap.acount) || string.IsNullOrEmpty(ap.password)) return (false, "账号或密码为空");


            return _client.Login(new Client.core.boot.Configuration
            {
                LoginAccount = ap.acount,
                LoginPassWord = ap.password,
                ChannelType = club.ChannelType,
                IsAdministrator = isAdministrator,
                LoginNeedArenaAttribs = isJustAttribs,
                LoginJustGWEvents = club.LoginSimpleMode,
                TargetClubRID = rid,
                IsAutoLogin = isAuto,
                TimeoutInterval = _context.ConfigsM.ClubLoginTimeoutInterval,


            });
        }
        /// <summary>
        /// 启动一次登录
        /// </summary>
        /// <param name="rid">俱乐部RID</param>
        /// <param name="isJustAttribs">是否仅获取九维</param>
        /// <param name="isAdministrator">是否由超管发起</param>
        /// <param name="needRemind">是否需要提醒</param>
        /// <param name="scheduleTimeTag">自动登录时间节点（用于寻找自动登录表配置）</param>
        /// <param name="isAuto">是否是自动登录（影响提醒等）</param>
        /// <returns></returns>
        public (bool result, string? desc) StartLogin(string rid, bool isJustAttribs, bool isAdministrator, bool needRemind = false, string scheduleTimeTag = "", bool isAuto = false)
        {
            if (_context.ConfigsM.SwitchLoginClose) return (false, "当前已临时禁用登录功能");

            var club = _context.ClubsM.FindClub(rid);
            if (club is null) return (false, "未找到俱乐部");
            var ap = club.GetRandomAccountPassword();
            if (string.IsNullOrEmpty(ap.acount) || string.IsNullOrEmpty(ap.password)) return (false, "账号或密码为空");
            var weekday = TimeM.GetWeekDayStatic();
            var hour = DateTime.Now.Hour;
            // 手动启动所有   needRemind:按指令, isAuto:false
            // 手动启动指定   needRemind:按指令, isAuto:false
            // 自动启动       needRemind:忽略, isAuto:true, scheduleTimeTag:按指定
            bool needRemindTokens, needRemindMines, needRemindDrill, needRemindGW;

            var remindAsperSheet = club.IsAutoLoginTimeTagExist(scheduleTimeTag);
            AutoLoginConfig? autoLoginCfg = remindAsperSheet ? club.QueryAutoLoginConfig(scheduleTimeTag) : null;


            if (isAuto)
            {
                if (club.AutoLoginSheetApplied && autoLoginCfg is not null)
                {
                    // 已经启用配置表
                    needRemindTokens = club.RemindAfterLogin && autoLoginCfg.remindTokens;
                    needRemindMines = club.RemindAfterLogin && autoLoginCfg.remindMines;
                    needRemindDrill = club.RemindAfterLogin && autoLoginCfg.remindDrills;
                    needRemindGW = club.RemindAfterLogin && autoLoginCfg.remindGW;
                }
                else
                {
                    needRemindTokens = club.RemindAfterLogin && !club.DontRemindTokens && hour >= 19;
                    needRemindMines = club.RemindAfterLogin && !club.DontRemindMine;
                    needRemindDrill = club.RemindAfterLogin && weekday == 4 && hour >= 18;
                    needRemindGW = club.RemindAfterLogin && club.RemindContentContainsGroupWarHistory;
                }
            }
            else
            {
                needRemindTokens = needRemind && !club.DontRemindTokens && hour >= 19;
                needRemindMines = needRemind && !club.DontRemindMine;
                needRemindDrill = needRemind && weekday == 4 && hour >= 18;
                needRemindGW = needRemind && club.RemindContentContainsGroupWarHistory;
            }

            return _client.Login(new Client.core.boot.Configuration
            {
                LoginAccount = ap.acount,
                LoginPassWord = ap.password,
                ChannelType = club.ChannelType,
                IsAdministrator = isAdministrator,
                LoginNeedArenaAttribs = isJustAttribs,
                LoginJustGWEvents = club.LoginSimpleMode,
                TargetClubRID = rid,
                IsAutoLogin = isAuto,
                TimeoutInterval = _context.ConfigsM.ClubLoginTimeoutInterval,

                RemindTokens = needRemindTokens,
                RemindMines = needRemindMines,
                RemindDrills = needRemindDrill,
                RemindGW = needRemindGW,
            });
        }
        /// <summary>
        /// 停止登录
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public (bool result, string? desc) StopLogin(string rid = "") => _client.StopLogin(rid);
        /// <summary>
        /// 查询正在登录的信息
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryCurrentlyLoginInfo(string rid)
        {
            return _client.GetLoginStatus(rid);
        }

        #endregion

        #region CALLBACK
        /// <summary>
        /// 登录开始回调
        /// </summary>
        /// <param name="rid"></param>
        private void OnLoginStart(Client.core.boot.Configuration config)
        {
            var club = _context.ClubsM.FindClub(config.TargetClubRID);
            if (club is null) return;

            // 处理数据
            club.LastLoginTime = config.StartTime;

            // 俱乐部群通知
            var group = _context.ContactsM.FindGroupByRID(config.TargetClubRID);
            if (!_context.IsTest && group is not null)
            {
                var desc = "";
                if (club.AccountUIDs.ContainsKey(config.LoginAccount))
                {
                    var uid = club.AccountUIDs[config.LoginAccount];
                    var clubMember = _context.ClubsM.FindMember(uid);
                    if (clubMember is not null && !string.IsNullOrEmpty(clubMember.Name)) desc = clubMember.Name;
                    else desc = uid;
                }
                if (!string.IsNullOrEmpty(desc)) desc = $" ({desc})";
                desc = $"你的俱乐部正在{(config.IsAutoLogin ? "自动" : "")}登录账号 [{StringHelper.GetMixedPhoneNumber(config.LoginAccount)}]{desc} ({include.club.ChannelTypeDesc(club.ChannelType)})，" +
                       $"在完成之前请勿使用该账号登录游戏！";

                var holder = _context.ContactsM.QueryClubHolderWXID(config.TargetClubRID);

                if (club.LoginDontAtHolder || string.IsNullOrEmpty(holder)) _context.WechatM.SendText(desc, group.WXID);
                else _context.WechatM.SendAtText(desc, new List<string> { holder }, group.WXID);
            }
            // 通知群通知
            group = _context.ContactsM.FindGroup(_context.WechatM.WechatCFG()?.ManagerGroup ?? "");
            if (group is not null)
            {
                var desc = $"正在登录[{club.Name}] [{include.club.ChannelTypeDesc(club.ChannelType)}]";
                _context.WechatM.SendText(desc, group.WXID);
            }
        }

        /// <summary>
        /// 登录失败回调
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="fail"></param>
        private void OnLoginFailed(Client.core.boot.Configuration config, string fail, bool retry)
        {
            var club = _context.ClubsM.FindClub(config.TargetClubRID);
            if (club is null) return;
            club.LastLoginTime = config.EndTime;
            club.LastLoginUID = config.UID;

            config.RetryTimes++;
            var canRetry = retry && config.RetryTimes < 3;

            // 俱乐部群通知
            var group = _context.ContactsM.FindGroupByRID(config.TargetClubRID);
            if (!_context.IsTest && group is not null)
            {
                var desc = $"你的俱乐部登录失败了，原因如下：\n{fail}\n" +
                       (canRetry ? $"你的俱乐部将重新进入登录等待队列，进行第{config.RetryTimes}次重新登录" : $"请排查问题后再次尝试登录！");

                var holder = _context.ContactsM.QueryClubHolderWXID(config.TargetClubRID);

                if (club.LoginDontAtHolder || string.IsNullOrEmpty(holder)) _context.WechatM.SendText(desc, group.WXID);
                else _context.WechatM.SendAtText(desc, new List<string> { holder }, group.WXID);
            }
            // 通知群通知
            group = _context.ContactsM.FindGroup(_context.WechatM.WechatCFG()?.ManagerGroup ?? "");
            if (group is not null)
            {
                var desc = $"[{club.Name}]登录失败，原因：\n{fail}\n" +
                           $"本次登录时长：{TimeHelper.ChinsesTimeDurationDesc(config.EndTime - config.StartTime)}\n" +
                           _client.CommonWaitingDesc() +
                           (canRetry ? $"\n该俱乐部30s后将重新进入登录等待队列，进行第{config.RetryTimes}次重新登录" : "");
                _context.WechatM.SendText(desc, group.WXID);
            }

            if (canRetry)
            {
                Task.Run(() =>
                {
                    Thread.Sleep(30000);
                    _client.Login(config);
                });
            }
        }
        /// <summary>
        /// 登录完成回调
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="data"></param>
        private void OnLoginCompleted(Client.core.boot.Configuration config, dynamic data)
        {
            if (config is null) return;
            var club = _context.ClubsM.FindClub(config.TargetClubRID);
            if (club is null) return;
            club.LastLoginTime = config.EndTime;
            club.LastLoginUID = config.UID;

            // 处理数据
            _context.ClubsM.LoadLoginData(config.TargetClubRID, data);

            // 俱乐部群通知
            var group = _context.ContactsM.FindGroupByRID(config.TargetClubRID);
            if (group is not null)
            {
                var desc = $"你的俱乐部登录完成了。";
                var holder = _context.ContactsM.QueryClubHolderWXID(config.TargetClubRID);
                if (club.LoginDontAtHolder || string.IsNullOrEmpty(holder)) _context.WechatM.SendText(desc, group.WXID);
                else _context.WechatM.SendAtText(desc, new List<string> { holder }, group.WXID);

                var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
                // 自动提醒
                if (!config.LoginNeedArenaAttribs && config.AnyRemindAfterLogin && weekday != 5)
                {
                    _context.ClubsM.RemindAfterLogin(config.TargetClubRID, group.WXID, holder, config);
                }
            }
            // 通知群通知
            group = _context.ContactsM.FindGroup(_context.WechatM.WechatCFG()?.ManagerGroup ?? "");
            if (group is not null)
            {

                var desc = $"[{club.Name}]登录完成\n" +
                           $"本次登录用时{TimeHelper.ChinsesTimeDurationDesc(config.EndTime - config.StartTime)}\n" +
                           _client.CommonWaitingDesc();
                _context.WechatM.SendText(desc, group.WXID);
            }

            _context.ClubsM.SaveCSV();
        }
        #endregion
    }
}
