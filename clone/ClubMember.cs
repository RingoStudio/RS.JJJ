using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Utils;

namespace RS.Snail.JJJ.clone
{
    /// <summary>
    /// 俱乐部成员
    /// </summary>
    internal class ClubMember : Dbase
    {
        #region FIELDS
        public const string TAG = "ClubMember";
        private boot.Context _context;

        public ClubMember(Context context, JObject dbase) : base(context, dbase?["dbase"] ?? new JObject())
        {
            _context = context;
            RID = JSONHelper.ParseString(dbase["rid"]);
            RobotWxid = JSONHelper.ParseString(dbase["robot_wxid"]);
            Name = CryptoHelper.DecryptBase64(JSONHelper.ParseString(dbase["name"]));
            Resume = JSONHelper.ParseStringList(dbase["resume"]);
            UpdateTime = JSONHelper.ParseLong(dbase["update_time"]);
        }

        public ClubMember(Context context, string rid, string name, string robotWxid) : base(context, new JObject())
        {
            _context = context;
            RID = rid;
            Name = name;
            RobotWxid = robotWxid;
        }
        #endregion

        #region PUBLIC FIELDS
        public string RID { get; private set; }
        public string Name { get; private set; }
        public string RobotWxid { get; private set; }
        public List<string> Resume { get; set; } = new List<string>();
        public long UpdateTime { get; private set; }

        public Kit Kit { get => (Kit)(IsWeekKitGetted() ? JSONHelper.ParseInt(Query("kit")) : 0); }
        public Kit KitAlloc
        {
            get => (Kit)(IsWeekKitAllocated() ? JSONHelper.ParseInt(Query("kit_alloc")) : 0);
            set
            {
                Set("kit_alloc", (int)value);
                Set("kit_alloc_time", TimeHelper.ToTimeStamp());
            }
        }
        /// <summary>
        /// 锁定套装
        /// </summary>
        public Kit KitLocked
        {
            get => (Kit)(JSONHelper.ParseInt(Query("kit_locked")));
            set => Set("kit_locked", (int)value);
        }
        #endregion

        #region METHODS
        public string NameOrUID() => string.IsNullOrEmpty(Name) ? RID : Name;
        /// <summary>
        /// 数据今天是否更新过
        /// </summary>
        /// <returns></returns>
        public bool IsTodayUpdated() => RS.Snail.JJJ.Client.core.game.module.TimeM.IsSameDurationStatic(UpdateTime, TimeHelper.ToTimeStamp(), 86400);
        public bool IsWeekUpdated() => RS.Snail.JJJ.Client.core.game.module.TimeM.IsSameDurationStatic(UpdateTime, TimeHelper.ToTimeStamp(), 7 * 86400);
        public bool IsWeekKitAllocated() => RS.Snail.JJJ.Client.core.game.module.TimeM.IsSameDurationStatic(JSONHelper.ParseLong(Query("kit_alloc_time")), TimeHelper.ToTimeStamp(), 7 * 86400);
        public bool IsWeekKitGetted() => RS.Snail.JJJ.Client.core.game.module.TimeM.IsSameDurationStatic(JSONHelper.ParseLong(Query("kit_time")), TimeHelper.ToTimeStamp(), 7 * 86400);
        public void UpdateData(dynamic data, bool isGW = false)
        {
            if (data is not JObject || JSONHelper.GetCount(data) <= 0) return;
            var now = TimeHelper.ToTimeStamp();
            if (!IsWeekUpdated()) Replace(new JObject());
            if (isGW) Set("gw", data);
            else Absorb(data);
            Name = CryptoHelper.DecryptBase64(JSONHelper.ParseString(Query("nickname")));
            this.UpdateTime = now;
        }
        public dynamic? Query(string path, dynamic? defaults = null) => Query(path, null, defaults);
        public dynamic GetJO()
        {
            return JObject.FromObject(new
            {
                name = CryptoHelper.EncryptBase64(Name),
                robot_wxid = RobotWxid,
                rid = RID,
                resume = Resume,
                dbase = dbase,
                update_time = UpdateTime,
            });
        }

        public void ChangeClubRID(string newRID = "")
        {
            var timeDesc = TimeHelper.ChinsesTimeDesc(TimeHelper.ToTimeStamp());
            if (string.IsNullOrEmpty(RID)) Resume.Add($"[{timeDesc}]离开原俱乐部");
            else
            {
                var club = _context.ClubsM.FindClub(RobotWxid, newRID);
                var name = "";
                if (club is not null) name = club.Name;
                if (string.IsNullOrEmpty(name)) name = newRID;
                else name = $"{name} {newRID}";

                Resume.Add($"[{timeDesc}]加入俱乐部[{name}]");
            }
        }
        #endregion
    }
}
