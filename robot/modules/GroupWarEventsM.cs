using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.modules
{
    internal class GroupWarEventsM : IModule
    {
        #region FIELDS
        public string ModuleName { get => "CdM"; }
        public Context _context { get; set; }

        private bool _inited = false;
        public bool Inited { get => _inited; }

        // cache 
        // <channel>:
        //  <dist_sort>:
        //      <share_key>:
        //          <>
        //      <empty_key>:

        private ConcurrentDictionary<string, ClubEvents> _events;
        #endregion

        #region INIT
        public GroupWarEventsM(boot.Context context)
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
            _context.BackupM.RemoveBackupSession(Tools.Common.Enums.CSVType.UserClub, include.files.GroupWar_Events_CSV);
        }
        private void LoadCSV()
        {
            _events = new();
            var data = IOHelper.GetCSV(Tools.Common.Enums.CSVType.UserClub, include.files.GroupWar_Events_CSV) ?? new JObject();
            foreach (var item in data)
            {
                var one = new ClubEvents(item.Value);
                if (!one.IsUpdatedThisWeek()) continue;
                _events[one.Tag] = one;
            }
        }

        private void SaveCSV()
        {
            dynamic jo = new JObject();
            foreach (var item in _events)
            {
                jo[item.Key] = item.Value.GetJO();
            }
            try
            {
                IOHelper.SaveCSV(Tools.Common.Enums.CSVType.UserClub, jo, include.files.GroupWar_Events_CSV);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "CdM.SaveCSV");
            }
        }
        #endregion

        #region PRIVATE METHODS
        /// <summary>
        /// 获得缓存KEY
        /// </summary>
        /// <param name="channelType"></param>
        /// <param name="distSort"></param>
        /// <param name="sharekey"></param>
        /// <returns></returns>
        private string GetKey(ChannelType channelType, int distSort, string sharekey = "")
        {
            return $"{channelType}_{distSort}_{sharekey ?? ""}";
        }

        #endregion

        #region METHODS
        /// <summary>
        /// 更新物种事件
        /// </summary>
        /// <param name="data"></param>
        /// <param name="channelType"></param>
        /// <param name="distSort"></param>
        /// <param name="shareKey"></param>
        public void Update(dynamic data, ChannelType channelType, int distSort, string shareKey = "")
        {
            var key = GetKey(channelType, distSort, shareKey);
            if (!_events.ContainsKey(key))
            {
                var one = new ClubEvents(channelType, distSort, shareKey);
                _events[key] = one;
            }
            _events[key].Update(data);
        }
        /// <summary>
        /// 查询物种事件
        /// </summary>
        /// <param name="channelType"></param>
        /// <param name="distSort"></param>
        /// <param name="shareKey"></param>
        /// <param name="area"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<ClubEvent> QueryEvents(ChannelType channelType, int distSort, string shareKey = "", int area = 0, int type = 0)
        {
            var key = GetKey(channelType, distSort, shareKey);
            var list = _events.ContainsKey(key) ? _events[key].QueryEvents(area, type) : new List<ClubEvent>();
            if (!string.IsNullOrEmpty(key))
            {
                key = GetKey(channelType, distSort, "");
                var list2 = _events.ContainsKey(key) ? _events[key].QueryEvents(area, type) : new List<ClubEvent>();
                if (list2.Count > 0)
                {
                    if (list.Count == 0) list.AddRange(list2);
                    else
                    {
                        var poses = list.Select(a => a.Pos).ToList();
                        foreach (var item in list2)
                        {
                            if (!poses.Contains(item.Pos)) list.Add(item);
                        }
                    }
                }
            }

            return list;
        }
        /// <summary>
        /// 查询物种事件
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="area"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public string? QueryEventsDesc(string rid, int area = 0, int type = 0)
        {
            var club = _context.ClubsM.FindClub(rid);
            if (club is null) return null;

            var list = QueryEvents(club.ChannelType, club.CombinedDistSort, club.GroupwarEventShareKey ?? "", area, type);
            if (list.Count == 0) return null;

            //if (list.Count > 30)
            //{
            //    return $"查询到 {list.Count} 个事件，数量太多\n" +
            //           $"请使用更多限定条件查询，例如\"查询物种事件 16 多人\"\n" +
            //           $"或者通知本群的经理发送\"生成事件总图\"";
            //}

            var curEvents = club.GroupWarData.Query("events") ?? new JObject();

            var ret = list.Select(a => $"[{a.Pos}]{a.Name} {(curEvents[a.Pos] is null ? " (未挖掘)" : "")}");

            return $"为你查询到以下 {ret.Count()} 个事件：\n" +
                   string.Join("\n", ret);
        }

        public int ConvertEventType(string str) => str.Replace("事件", "").ToLower() switch
        {
            "多人" or "公共" or "multi" => RS.Snail.JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MULTI,
            "单人" or "个人" or "single" => RS.Snail.JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_SINGLE,
            "矿点" or "矿" or "挖矿" or "采矿" or "采集" or "mine" => RS.Snail.JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MINE,
            "大怪" or "王" or "boss" => RS.Snail.JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_BOSS,
            "小怪" or "monster" => RS.Snail.JJJ.Client.core.game.include.group_war.GW_ELEMENT_TYPE_MONSTER,
            _ => -1,

        };
        #endregion

    }
}
