using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.robot.cmd.misc;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.clone
{
    internal class Club
    {
        [JsonIgnore]
        private Context _context;

        #region CONFIGS
        /// <summary>
        /// 俱乐部IRD
        /// </summary>
        public string RID { get; private set; }

        [JsonIgnore]
        private string _name = "";
        /// <summary>
        /// 俱乐部名称
        /// </summary>
        public string Name
        {
            get
            {
                var ourName = _context.WechatM.WechatCFG()?.OurName ?? "唧唧叽";
                if (string.IsNullOrEmpty(_name)) return $"{ourName}俱乐部";
                if (ourName == "冰法集团" && !_name.Contains("冰法")) return $"冰法{_name}";
                return _name;
            }
            set => _name = value;
        }

        public Dictionary<string, string> AccountPasswords { get; set; }
        public Dictionary<string, string> AccountUIDs { get; set; } = new();

        /// <summary>
        /// 俱乐部成员名单
        /// </summary>
        public List<string> Members { get; set; }
        /// <summary>
        /// 上次登录UID
        /// </summary>
        public string LastLoginUID { get; set; } = string.Empty;
        /// <summary>
        /// 上次登录时间
        /// </summary>
        public long LastLoginTime { get; set; }
        /// <summary>
        /// 渠道类型
        /// </summary>
        public ChannelType ChannelType { get; private set; }
        /// <summary>
        /// 是否启用自动登录配置表
        /// </summary>
        public bool AutoLoginSheetApplied { get; set; }
        public List<AutoLoginConfig> AutoLoginConfigs { get; set; }
        #endregion

        #region SETTINGS
        /// <summary>
        /// 是否禁用提醒内容合并模式
        /// </summary>
        public bool RemindContentNotCombine { get; set; } = false;
        /// <summary>
        /// 是否开启登录后自动提醒
        /// </summary>
        public bool RemindAfterLogin { get; set; }
        /// <summary>
        /// 是否日常提示内容包括物种战争历史玩法信息
        /// </summary>
        public bool RemindContentContainsGroupWarHistory { get; set; } = false;
        /// <summary>
        /// 物种事件共享密钥
        /// </summary>
        public string GroupwarEventShareKey { get; set; } = "";
        /// <summary>
        /// 是否是简易登录（不含boss、历史等）
        /// </summary>
        public bool LoginSimpleMode { get; set; } = false;
        /// <summary>
        /// 登录信息不at会长
        /// </summary>
        public bool LoginDontAtHolder { get; set; }
        /// <summary>
        /// 是否参与大循环
        /// </summary>
        public bool LoginAuto { get; set; }
        ///// <summary>
        ///// 登录是否只需要兵种战力
        ///// </summary>
        //public bool LoginNeedSonArmy { get; set; }
        /// <summary>
        /// 套装规划形式
        /// </summary>
        public ClubKitPlanType ClubKitPlanType { get; set; } = ClubKitPlanType.SONCOMBAT;
        /// <summary>
        /// 登录排序
        /// </summary>
        public int LoginSort { get; set; } = 0;
        /// <summary>
        /// 不提醒群成员变动
        /// </summary>
        public bool DontRemindGroupMemberChanged { get; set; }
        /// <summary>
        /// 不提醒俱乐部成员变动
        /// </summary>
        public bool DontRemindClubMemberChanged { get; set; }
        /// <summary>
        /// 北极贝周不需要买白蝌蚪卷轴
        /// </summary>
        public bool Spe3DontNeedBuyGem { get; set; }
        /// <summary>
        /// 仓鼠周不需要参与拍卖
        /// </summary>
        public bool Spe4DontNeedAuction { get; set; }
        /// <summary>
        /// 不需要提醒挖矿即将到期
        /// </summary>
        public bool DontRemindMineClose { get; set; }
        /// <summary>
        /// 布阵图显示战力数值
        /// </summary>
        public bool KitImageShowValue { get; set; }
        /// <summary>
        /// 不需要自动提醒挖矿
        /// </summary>
        public bool DontRemindMine { get; set; }
        /// <summary>
        /// 不需要自动提醒挖矿
        /// </summary>
        public bool DontRemindTokens { get; set; }

        /// <summary>
        /// 提醒钻头剩余下限
        /// </summary>
        public int RemindDrillLowerLimit { get; set; }
        /// <summary>
        /// 钻头达标数(多)
        /// </summary>
        public int DrillUseLimit1 { get; set; }
        /// <summary>
        /// 钻头达标数(少)
        /// </summary>
        public int DrillUseLimit2 { get; set; }
        #endregion

        #region DATA
        /// <summary>
        /// 区服描述
        /// </summary>
        public string DistDesc { get; set; } = "";
        /// <summary>
        /// 区服号
        /// </summary>
        public int DistSort { get; set; }
        /// <summary>
        /// 合区大区
        /// </summary>
        public int CombinedDistSort
        {
            get => (DistSort - 1) / (DistCombine == 0 ? 64 : DistCombine) + 1;
        }
        /// <summary>
        /// 合区数量
        /// </summary>
        public int DistCombine { get; set; }
        /// <summary>
        /// 物种地图
        /// </summary>
        public string Map { get; set; }
        /// <summary>
        /// 信息更新时间
        /// </summary>
        public long UpdateTime { get; set; }

        public Dbase GroupWarData { get; set; }

        public List<string> GroupWarSpe4AuctionPoses { get; set; }
        public long GroupWarSpe4AuctionPosesUpdateTime { get; set; }
        /// <summary>
        /// 伤害记录
        /// </summary>
        public dynamic CombatRecord { get; set; }
        public long KitPlanTime { get; set; }
        public int Level { get; set; }
        public int PropMembersNum { get; set; }
        public int MaxMemberNum
        {
            get
            {
                int num = 60 + (Level / 5) * 5;
                if (Level > 20) num += (Level - 20) / 5 * 5;
                return num + PropMembersNum;
            }
        }
        #endregion

        #region PURCHASE
        /// <summary>
        /// 初次订阅
        /// </summary>
        public long PurchaseStart { get; set; }
        /// <summary>
        /// 订阅到期
        /// </summary>
        public long PurchaseEnd { get; set; }
        #endregion

        #region INIT
        public Club(Context context, string rid, ChannelType chennelType)
        {
            _context = context;
            RID = rid;
            ChannelType = chennelType;
            Members = new List<string>();
            GroupWarData = new Dbase(_context, new JObject());
            PurchaseStart = TimeHelper.ToTimeStamp();
            PurchaseEnd = PurchaseStart;
        }

        public Club(Context context, dynamic data)
        {
            _context = context;
            if (data is not JObject) return;
            RID = JSONHelper.ParseString(data.rid);
            _name = CryptoHelper.DecryptBase64(JSONHelper.ParseString(data.name));
            Members = JSONHelper.ParseStringList(data.members) ?? new List<string>();
            LastLoginTime = JSONHelper.ParseLong(data.last_login_time);
            LastLoginUID = JSONHelper.ParseString(data.last_login_uid);
            ChannelType = (ChannelType)JSONHelper.ParseInt(data.channel_type);

            RemindContentNotCombine = JSONHelper.ParseBool(data.remind_content_not_combine);
            RemindAfterLogin = JSONHelper.ParseBool(data.remind_after_login, true);
            RemindContentContainsGroupWarHistory = JSONHelper.ParseBool(data.remind_content_contains_group_war_history, true);
            GroupwarEventShareKey = CryptoHelper.DecryptBase64(JSONHelper.ParseString(data.group_war_event_share_key));
            LoginSimpleMode = JSONHelper.ParseBool(data.login_simple_mode);
            ClubKitPlanType = (ClubKitPlanType)JSONHelper.ParseInt(data.club_kit_plan_type);
            LoginDontAtHolder = JSONHelper.ParseBool(data.login_dont_at_holder);
            LoginAuto = JSONHelper.ParseBool(data.login_auto);
            Spe3DontNeedBuyGem = JSONHelper.ParseBool(data.spe3_dont_need_buy_gem);
            Spe4DontNeedAuction = JSONHelper.ParseBool(data.spe4_dont_need_auction);
            DontRemindMineClose = JSONHelper.ParseBool(data.dont_remind_mine_close);
            KitImageShowValue = JSONHelper.ParseBool(data.kit_image_show_value);

            GroupWarSpe4AuctionPoses = JSONHelper.ParseStringList(data.group_war_spe_4_auction_poses);
            GroupWarSpe4AuctionPosesUpdateTime = JSONHelper.ParseLong(data.group_war_spe_4_auction_poses_update_time);

            DontRemindClubMemberChanged = JSONHelper.ParseBool(data.dont_remind_club_member_changed);
            DontRemindGroupMemberChanged = JSONHelper.ParseBool(data.dont_remind_group_member_changed);
            DontRemindMine = JSONHelper.ParseBool(data.dont_remind_mine);
            DontRemindTokens = JSONHelper.ParseBool(data.dont_remind_tokens);
            RemindDrillLowerLimit = JSONHelper.ParseInt(data.remind_drill_lower_limit);
            DrillUseLimit1 = JSONHelper.ParseInt(data.drill_use_limit_1);
            DrillUseLimit2 = JSONHelper.ParseInt(data.drill_use_limit_2);

            DistDesc = JSONHelper.ParseString(data.dist_desc);
            DistSort = JSONHelper.ParseInt(data.dist_sort);
            DistCombine = JSONHelper.ParseInt(data.dist_combine);

            Map = JSONHelper.ParseString(data.map);
            UpdateTime = JSONHelper.ParseLong(data.update_time);

            AccountPasswords = JSONHelper.ParseDicStrStr(data.account_passwords);
            AccountUIDs = JSONHelper.ParseDicStrStr(data.account_uids);

            GroupWarData = new Dbase(_context, data.group_war_data ?? new JObject());

            CombatRecord = data.combat_record ?? new JObject();

            KitPlanTime = JSONHelper.ParseLong(data.kit_plan_time);

            PurchaseStart = JSONHelper.ParseLong(data.purchase_start);
            PurchaseEnd = JSONHelper.ParseLong(data.purchase_end);
            LoginSort = JSONHelper.ParseInt(data.login_sort);
            Level = JSONHelper.ParseInt(data.level);
            PropMembersNum = JSONHelper.ParseInt(data.prop_members_num);

            AutoLoginSheetApplied = JSONHelper.ParseBool(data.auto_login_sheet_applied);
            AutoLoginConfigs = new();
            if (AutoLoginSheetApplied && data.auto_login_configs is not null)
            {
                foreach (var config in data.auto_login_configs)
                {
                    AutoLoginConfigs.Add(new(config));
                }
            }
        }
        #endregion

        #region METHODS
        /// <summary>
        /// 数据今天是否更新过
        /// </summary>
        /// <returns></returns>
        public bool IsTodayUpdated() => RS.Snail.JJJ.Client.core.game.module.TimeM.IsSameDurationStatic(UpdateTime, TimeHelper.ToTimeStamp(), 86400);
        /// <summary>
        /// 本周是否更新过数据
        /// </summary>
        /// <returns></returns>
        public bool IsWeekUpdated() => RS.Snail.JJJ.Client.core.game.module.TimeM.IsSameDurationStatic(UpdateTime, TimeHelper.ToTimeStamp(), 7 * 86400);
        /// <summary>
        /// 本周是否排过套装
        /// </summary>
        /// <returns></returns>
        public bool IsWeekKitPlanned() => RS.Snail.JJJ.Client.core.game.module.TimeM.IsSameDurationStatic(KitPlanTime, TimeHelper.ToTimeStamp(), 7 * 86400);

        /// <summary>
        /// 登录后更新信息
        /// </summary>
        /// <param name="data"></param>
        public void UpdateData(dynamic data)
        {
            if (data is not JObject || JSONHelper.GetCount(data) <= 0) return;
            this.Name = JSONHelper.ParseString(data.name);
            this.DistSort = JSONHelper.ParseInt(data.dist_sort);
            this.DistDesc = JSONHelper.ParseString(data.dist_dest);
            this.DistCombine = JSONHelper.ParseInt(data.dist_combine);
            this.Map = JSONHelper.ParseString(data.map);
            var account = JSONHelper.ParseString(data.my_account);
            var uid = JSONHelper.ParseString(data.my_uid);
            if (AccountPasswords.ContainsKey(account)) AccountUIDs[account] = uid;
            CombatRecord = data.combat_record ?? new JObject();

            this.UpdateTime = TimeHelper.ToTimeStamp();
        }
        /// <summary>
        /// 更新物种战争数据
        /// </summary>
        /// <param name="data"></param>
        public void UpdateGWData(dynamic data)
        {
            if (data is not JObject || JSONHelper.GetCount(data) <= 0) return;
            var now = TimeHelper.ToTimeStamp();
            if (IsWeekUpdated()) this.GroupWarData.Absorb(data);
            else this.GroupWarData.dbase = data;
            this.UpdateTime = now;
        }

        public void UpdateGWEvents(dynamic data)
        {
            if (data is not JObject) data = new JObject();
            this.GroupWarData.Set("events", data);
        }

        public bool IsPurchased() => PurchaseEnd == long.MinValue || PurchaseEnd > TimeHelper.ToTimeStamp();
        /// <summary>
        /// 上次登录记录
        /// </summary>
        /// <returns></returns>
        public string LastLoginDesc()
        {
            if (string.IsNullOrEmpty(LastLoginUID)) return "无记录";
            var user = _context.ClubsM.FindMember(LastLoginUID);
            var name = "";
            if (user is not null) name = $"{user.Name}";
            else name = LastLoginUID;
            name = $"{name} {TimeHelper.ChinsesTimeDesc(LastLoginTime)}";

            return name;
        }
        /// <summary>
        /// 登录账号简介
        /// </summary>
        /// <returns></returns>
        public string LoginAccountDesc()
        {
            if (AccountPasswords.Count <= 0) return $"俱乐部[{Name} {RID}]没有设置任何登录账号。";
            var list = new List<string> { $"俱乐部[{Name} {RID}]共有 {AccountPasswords.Count} 个登录账号：" };
            foreach (var item in AccountPasswords)
            {
                var account = item.Key;
                if (AccountUIDs.ContainsKey(account))
                {
                    var uid = AccountUIDs[account];
                    var user = _context.ClubsM.FindMember(uid);
                    if (user is not null) account = $"{user.Name}[{account}]";
                    else account = $"{account}[{uid}]";
                }
                list.Add(StringHelper.GetMixedPhoneNumber(account));
            }
            return string.Join("\n", list);
        }

        /// <summary>
        /// 取随机一对账号密码
        /// </summary>
        /// <returns></returns>
        public (string acount, string password) GetRandomAccountPassword()
        {
            if (AccountPasswords is null || AccountPasswords.Count == 0) return ("", "");
            var rand = new Random().Next(AccountPasswords.Count);
            var account = AccountPasswords.Keys.ToArray()[rand];
            return (account, AccountPasswords[account]);
        }
        /// <summary>
        /// 更新成员名单
        /// </summary>
        /// <param name="latestMembers"></param>
        /// <returns></returns>
        public (List<string> curMembers, List<string> newMembers, List<string> removedMembers) UpdateMembers(List<string> latestMembers)
        {
            var newMembers = new List<string>();
            var removedMembers = new List<string>();
            foreach (var member in latestMembers)
            {
                if (!Members.Contains(member)) newMembers.Add(member);
            }

            foreach (var member in Members)
            {
                if (!latestMembers.Contains(member)) removedMembers.Add(member);
            }

            Members = latestMembers;

            return (Members, newMembers, removedMembers);
        }

        public dynamic GetJO()
        {
            return JObject.FromObject(new
            {
                rid = RID,
                name = CryptoHelper.EncryptBase64(_name),
                members = Members,
                last_login_time = LastLoginTime,
                last_login_uid = LastLoginUID,
                channel_type = (int)ChannelType,

                remind_content_not_combine = RemindContentNotCombine,
                remind_after_login = RemindAfterLogin,
                remind_content_contains_group_war_history = RemindContentContainsGroupWarHistory,
                group_war_event_share_key = CryptoHelper.EncryptBase64(GroupwarEventShareKey),
                login_simple_mode = LoginSimpleMode,
                club_kit_plan_type = (int)ClubKitPlanType,
                login_dont_at_holder = LoginDontAtHolder,
                group_war_data = GroupWarData.dbase ?? new JObject(),
                login_auto = LoginAuto,
                spe3_dont_need_buy_gem = Spe3DontNeedBuyGem,
                spe4_dont_need_auction = Spe4DontNeedAuction,
                dont_remind_mine_close = DontRemindMineClose,
                kit_image_show_value = KitImageShowValue,

                group_war_spe_4_auction_poses = GroupWarSpe4AuctionPoses,
                group_war_spe_4_auction_poses_update_time = GroupWarSpe4AuctionPosesUpdateTime,

                dont_remind_club_member_changed = DontRemindClubMemberChanged,
                dont_remind_group_member_changed = DontRemindGroupMemberChanged,
                dont_remind_mine = DontRemindMine,
                dont_remind_tokens = DontRemindTokens,
                remind_drill_lower_limit = RemindDrillLowerLimit,
                drill_use_limit_1 = DrillUseLimit1,
                drill_use_limit_2 = DrillUseLimit2,

                account_passwords = AccountPasswords,
                account_uids = AccountUIDs,

                update_time = UpdateTime,
                map = Map,

                dist_desc = DistDesc,
                dist_sort = DistSort,
                dist_combine = DistCombine,

                combat_record = CombatRecord,

                kit_plan_time = KitPlanTime,

                purchase_start = PurchaseStart,
                purchase_end = PurchaseEnd,
                login_sort = LoginSort,
                level = Level,
                prop_members_num = PropMembersNum,

                auto_login_sheet_applied = AutoLoginSheetApplied,
                auto_login_configs = ((AutoLoginConfigs is null || AutoLoginConfigs.Count == 0) ? new JArray() : JArray.FromObject(AutoLoginConfigs.Select(x => x.GetJO()))),
            });
        }
        #endregion

        #region AUTO LOGIN
        public bool IsAutoLoginTimeTagExist(string tag)
        {
            if (AutoLoginConfigs is null || AutoLoginConfigs.Count == 0) return false;
            return AutoLoginConfigs.Where(x => x.GetScheduleTimeTag() == tag).Any();
        }

        public AutoLoginConfig? QueryAutoLoginConfig(string tag)
        {
            if (AutoLoginConfigs is null || AutoLoginConfigs.Count == 0) return null;
            return AutoLoginConfigs.Where(x => x.GetScheduleTimeTag() == tag).FirstOrDefault();
        }
        public List<string> AllAutoLoginTimeTag() => AutoLoginConfigs.Where(x => x.isEnable)
                                                                     .Select(x => x.GetScheduleTimeTag()).ToList();
        public string GetAutoLoginConfigsDesc()
        {
            if (AutoLoginConfigs is null || AutoLoginConfigs.Count == 0) return "俱乐部自动登录配置表为空，目前无法进行自动登录。";
            var ret = new List<string>();
            ret.Add("俱乐部自动登录配置表详情:\n(●表示提醒金银牌，■表示提醒挖矿，▲表示提醒钻头，⬟表示提醒物种历史)");
            foreach (var item in AutoLoginConfigs)
            {
                if (!item.isEnable) continue;
                ret.Add(item.GetDesc());
            }

            return string.Join("\n", ret);
        }
        #endregion
    }

    internal class AutoLoginConfig
    {
        public int weekday;
        public int index;
        public string time;
        public bool isEnable;
        public bool remindTokens;
        public bool remindMines;
        public bool remindDrills;
        public bool remindGW;

        public AutoLoginConfig(dynamic data)
        {
            weekday = JSONHelper.ParseInt(data.weekday);
            index = JSONHelper.ParseInt(data.index);
            time = JSONHelper.ParseString(data.time);
            isEnable = JSONHelper.ParseBool(data.isEnable);
            remindTokens = JSONHelper.ParseBool(data.remindTokens);
            remindMines = JSONHelper.ParseBool(data.remindMines);
            remindDrills = JSONHelper.ParseBool(data.remindDrills);
            remindGW = JSONHelper.ParseBool(data.remindGW);

            isEnable = isEnable && CheckTimeVal();
        }
        public dynamic GetJO()
        {
            dynamic ret = new JObject();
            ret.weekday = weekday;
            ret.index = index;
            ret.time = time;
            ret.isEnable = isEnable;
            ret.remindTokens = remindTokens;
            ret.remindMines = remindMines;
            ret.remindDrills = remindDrills;
            ret.remindGW = remindGW;
            return ret;
        }
        public bool CheckTimeVal()
        {
            if (string.IsNullOrEmpty(time)) return false;
            var arr = time.Split(":");
            if (arr.Length < 2) return false;
            foreach (var item in arr)
            {
                if (!StringHelper.IsInt(item)) return false;
            }
            var hour = Convert.ToInt32(arr[0]);
            var minute = Convert.ToInt32(arr[1]);
            int sec = 0;
            if (arr.Length > 2) sec = Convert.ToInt32(arr[2]);
            return (hour >= 0 && hour <= 24 && minute >= 0 && minute <= 59 && sec >= 0 && sec <= 59);
        }

        public string GetScheduleTimeTag()
        {
            if (!isEnable) return "";
            var arr = time.Split(":");
            var hour = Convert.ToInt32(arr[0]);
            var minute = arr.Length > 1 ? Convert.ToInt32(arr[1]) : 0;
            var sec = arr.Length > 2 ? Convert.ToInt32(arr[2]) : 0;
            return $"{weekday}-{hour}-{minute}-{sec}";
        }
        public string GetDesc()
        {

            return $"{misc.WeekdayDesc(weekday)}{time} {(remindTokens ? "●" : "")}{(remindMines ? "■" : "")}{(remindDrills ? "▲" : "")}{(remindGW ? "⬟" : "")}";
        }

    }
}
