using Google.Protobuf.WellKnownTypes;
using RS.Snail.JJJ.boot;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.modules
{
    internal class SnailsM : IModule
    {
        #region FIELDS
        public string ModuleName { get => "SnailsM"; }
        public Context _context { get; set; }

        private bool _inited = false;
        public bool Inited { get => _inited; }

        private RS.Snail.JJJ.Client.Client _client;

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

            _client = new Client.Client(4)
            {
                OnLoginStart = OnLoginStart,
                OnLoginComplete = OnLoginCompleted,
                OnLoginFail = OnLoginFailed,
            };

            RegistAutoLogin();
        }

        private void RegistAutoLogin()
        {
            _context.ScheduleM.RegistSession(ModuleName,
                                            times: new List<string>
                                            {
                                                "6-12-30-0",
                                                "6-18-0-0",
                                                "6-21-0-0",

                                                "7-12-30-0",
                                                "7-18-0-0",
                                                "7-21-0-0",

                                                "1-12-30-0",
                                                "1-18-0-0",
                                                "1-21-0-0",

                                                "2-12-30-0",
                                                "2-18-0-0",
                                                "2-21-0-0",

                                                "3-12-30-0",
                                                "3-18-0-0",
                                                "3-21-0-0",

                                                "4-12-30-0",
                                                "4-18-0-0",
                                                "4-21-0-0",
                                            },
                                            action: new Action(() =>
                                            {
                                                foreach (var robot in _context.WechatM.GetWechatRobotWXIDs())
                                                {
                                                    StartLoginAll(robot, true);
                                                }
                                            }),
                                            isAutoLogin: true);
        }

        // TODO: 
        // 登录重启继续
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// 启动登录一个机器人下的所有账号
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <returns></returns>
        public Dictionary<string, (bool result, string? desc)> StartLoginAll(string robotWxid, bool isAuto = false)
        {
            var ret = new Dictionary<string, (bool result, string? desc)>();
            var rids = _context.ClubsM.GetLoginRIDs(robotWxid, isAuto);
            foreach (var rid in rids)
            {
                ret.Add(rid, StartLogin(robotWxid, rid, isAuto));
            }

            return ret;
        }
        /// <summary>
        /// 请求登录
        /// </summary>
        /// <param name="robotWxid">机器人id</param>
        /// <param name="rid">俱乐部rid</param>
        /// <param name="isJustSonArmy">是否仅获取兵种战力</param>
        /// <param name="isAdministrator">是否超管权限</param>
        /// <returns></returns>
        public (bool result, string? desc) StartLogin(string robotWxid, string rid, bool isJustSonArmy = false, bool isAdministrator = false, bool isAuto = false)
        {
            if (_context.ConfigsM.SwitchLoginClose)
            {
                return (false, "当前已临时禁用登录功能");
            }

            var club = _context.ClubsM.FindClub(robotWxid, rid);
            if (club is null) return (false, "未找到俱乐部");
            var ap = club.GetRandomAccountPassword();
            if (string.IsNullOrEmpty(ap.acount) || string.IsNullOrEmpty(ap.password)) return (false, "账号或密码为空");


            return _client.Login(new Client.core.boot.Configuration
            {
                LoginAccount = ap.acount,
                LoginPassWord = ap.password,
                ChannelType = club.ChannelType,
                IsAdministrator = isAdministrator,
                LoginNeedSonArmy = isJustSonArmy,
                LoginJustGWEvents = club.LoginSimpleMode,
                TargetClubRID = rid,
                RobotWxid = robotWxid,
                IsAutoLogin = isAuto,
            });
        }

        public (bool result, string? desc) StopLogin(string rid = "") => _client.StopLogin(rid);
        /// <summary>
        /// 查询正在登录的信息
        /// </summary>
        /// <param name="robotWxid"></param>
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
        /// <param name="robotWxid"></param>
        /// <param name="rid"></param>
        private void OnLoginStart(Client.core.boot.Configuration config)
        {
            var club = _context.ClubsM.FindClub(config.RobotWxid, config.TargetClubRID);
            if (club is null) return;

            // 处理数据
            club.LastLoginTime = config.StartTime;

            // 俱乐部群通知
            var group = _context.ContactsM.FindGroupByRID(config.RobotWxid, config.TargetClubRID);
            if (!_context.IsTest && group is not null)
            {
                var desc = "";
                if (club.AccountUIDs.ContainsKey(config.LoginAccount))
                {
                    var uid = club.AccountUIDs[config.LoginAccount];
                    var clubMember = _context.ClubsM.FindMember(config.RobotWxid, uid);
                    if (clubMember is not null && !string.IsNullOrEmpty(clubMember.Name)) desc = clubMember.Name;
                    else desc = uid;
                }
                if (!string.IsNullOrEmpty(desc)) desc = $" ({desc})";
                desc = $"你的俱乐部正在登录账号 [{StringHelper.GetMixedPhoneNumber(config.LoginAccount)}]{desc} ({include.club.ChannelTypeDesc(club.ChannelType)})，" +
                       $"在完成之前请勿使用该账号登录游戏！";

                var holder = _context.ContactsM.QueryClubHolder(config.RobotWxid, config.TargetClubRID);

                if (club.LoginDontAtHolder || string.IsNullOrEmpty(holder)) _context.WechatM.SendText(desc, config.RobotWxid, group.WXID);
                else _context.WechatM.SendAtText(desc, new List<string> { holder }, config.RobotWxid, group.WXID);
            }
            // 通知群通知
            group = _context.ContactsM.FindGroup(config.RobotWxid, _context.WechatM.FindWechatCFG(config.RobotWxid)?.ManagerGroup ?? "");
            if (group is not null)
            {
                var desc = $"正在登录[{club.Name}] [{include.club.ChannelTypeDesc(club.ChannelType)}]";
                _context.WechatM.SendText(desc, config.RobotWxid, group.WXID);
            }
        }

        /// <summary>
        /// 登录失败回调
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="rid"></param>
        /// <param name="fail"></param>
        private void OnLoginFailed(Client.core.boot.Configuration config, string fail)
        {
            var club = _context.ClubsM.FindClub(config.RobotWxid, config.TargetClubRID);
            if (club is null) return;

            config.RetryTimes++;
            var canRetry = config.RetryTimes < 3;

            // 俱乐部群通知
            var group = _context.ContactsM.FindGroupByRID(config.RobotWxid, config.TargetClubRID);
            if (!_context.IsTest && group is not null)
            {
                var desc = $"你的俱乐部登录失败了，原因如下：\n{fail}\n" +
                       (canRetry ? $"你的俱乐部将重新进入登录等待队列，进行第{config.RetryTimes}次重新登录" : $"请排查问题后再次尝试登录！");

                var holder = _context.ContactsM.QueryClubHolder(config.RobotWxid, config.TargetClubRID);

                if (club.LoginDontAtHolder || string.IsNullOrEmpty(holder)) _context.WechatM.SendText(desc, config.RobotWxid, group.WXID);
                else _context.WechatM.SendAtText(desc, new List<string> { holder }, config.RobotWxid, group.WXID);
            }
            // 通知群通知
            group = _context.ContactsM.FindGroup(config.RobotWxid, _context.WechatM.FindWechatCFG(config.RobotWxid)?.ManagerGroup ?? "");
            if (group is not null)
            {
                var desc = $"[{club.Name}]登录失败，原因：\n{fail}\n" +
                           $"本次登录时长：{TimeHelper.ChinsesTimeDurationDesc(config.EndTime - config.StartTime)}\n" +
                           _client.CommonWaitingDesc() +
                           (canRetry ? $"\n该俱乐部将重新进入登录等待队列，进行第{config.RetryTimes}次重新登录" : "");
                _context.WechatM.SendText(desc, config.RobotWxid, group.WXID);
            }

            if (canRetry)
            {
                _client.Login(config);
            }
        }
        /// <summary>
        /// 登录完成回调
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="rid"></param>
        /// <param name="data"></param>
        private void OnLoginCompleted(Client.core.boot.Configuration config, dynamic data)
        {
            if (config is null) return;
            var club = _context.ClubsM.FindClub(config.RobotWxid, config.TargetClubRID);
            if (club is null) return;

            // 处理数据
            _context.ClubsM.LoadLoginData(config.RobotWxid, config.TargetClubRID, data);

            // 俱乐部群通知
            var group = _context.ContactsM.FindGroupByRID(config.RobotWxid, config.TargetClubRID);
            if (group is not null)
            {
                var desc = $"你的俱乐部登录完成了。";
                var holder = _context.ContactsM.QueryClubHolder(config.RobotWxid, config.TargetClubRID);
                if (club.LoginDontAtHolder || string.IsNullOrEmpty(holder)) _context.WechatM.SendText(desc, config.RobotWxid, group.WXID);
                else _context.WechatM.SendAtText(desc, new List<string> { holder }, config.RobotWxid, group.WXID);
            }
            // 通知群通知
            group = _context.ContactsM.FindGroup(config.RobotWxid, _context.WechatM.FindWechatCFG(config.RobotWxid)?.ManagerGroup ?? "");
            if (group is not null)
            {
                var weekday = RS.Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();

                // 自动提醒
                if (config.IsAutoLogin && weekday != 5)
                {
                    var holder = _context.ContactsM.QueryClubHolder(config.RobotWxid, config.TargetClubRID);
                    Task.Run(() => _context.ClubsM.RemindAfterLogin(config.RobotWxid, config.TargetClubRID, group.WXID, holder));
                }
                else
                {
                    var desc = $"[{club.Name}]登录完成\n" +
                               $"本次登录用时{TimeHelper.ChinsesTimeDurationDesc(config.EndTime - config.StartTime)}\n" +
                               _client.CommonWaitingDesc();
                    _context.WechatM.SendText(desc, config.RobotWxid, group.WXID);
                }
            }

            _context.ClubsM.SaveCSV();
        }
        #endregion
    }
}
