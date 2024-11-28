using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.Client.core.game.module;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.clone
{
    internal class ClubEvents
    {
        public ChannelType ChannelType { get; private set; }
        public int DistSort { get; private set; }

        private Dictionary<string, ClubEvent> _events;
        public string ShareKey { get; private set; }

        public long UpdateTime { get; set; }

        public string Tag { get => $"{ChannelType}_{DistSort}_{ShareKey}"; }

        public ClubEvents()
        {
            _events = new();
        }
        public ClubEvents(ChannelType channelType, int distSort, string shareKey)
        {
            ChannelType = channelType;
            DistSort = distSort;
            _events = new();
            ShareKey = shareKey;
            UpdateTime = 0;
        }

        public ClubEvents(dynamic data)
        {
            ChannelType = (ChannelType)JSONHelper.ParseInt(data.channel_type);
            DistSort = JSONHelper.ParseInt(data.dist_sort);
            ShareKey = JSONHelper.ParseString(data.share_key);
            UpdateTime = JSONHelper.ParseLong(data.update_time);
            _events = new();
            foreach (var item in data.events ?? new JObject())
            {
                var one = new ClubEvent( item.Name, JSONHelper.ParseString(item.Value));
                _events.Add(one.Pos, one);
            }

        }



        public dynamic GetJO()
        {
            var dic = new Dictionary<string, string>();
            foreach (var item in _events)
            {
                dic[item.Key] = item.Value.Tag;
            }
            return JObject.FromObject(new
            {
                channel_type = ChannelType,
                dist_sort = DistSort,
                share_key = ShareKey,
                update_time = UpdateTime,
                events = dic,
            });
        }

        public void Update(dynamic data)
        {
            foreach (var item in data ?? new JObject())
            {
                var one = new ClubEvent(item.Name, JSONHelper.ParseString(item.Value));
                var pos = one.Pos;
                _events[pos] = one;
            }
            UpdateTime = TimeHelper.ToTimeStamp();
        }

        public bool IsUpdatedThisWeek() => RS.Snail.JJJ.Client.core.game.module.TimeM.IsSameDurationStatic(TimeHelper.ToTimeStamp(), UpdateTime, 7 * 86400);
        public List<ClubEvent> QueryEvents(int area = 0, int type = 0)
        {
            var list = new List<ClubEvent>();
            if (!IsUpdatedThisWeek()) return list;

            foreach (var item in _events.Values)
            {
                if (area > 0 && item.Area != area) continue;
                if (type > 0 && item.Type != type) continue;
                list.Add(item);
            }

            if (list.Count > 1) list.Sort((a, b) => a.Sort - b.Sort);
            return list;
        }
    }

    internal class ClubEvent
    {
        public int Area { get; private set; }
        public int Row { get; private set; }
        public int Col { get; private set; }
        public int Sort { get => Area * 10000 + Row * 100 + Col; }
        public int SortMark
        {
            get
            {
                var ret = Sort * 10;
                if (Name.Contains("α")) ret++;
                else if (Name.Contains("β")) ret += 2;
                else if (Name.Contains("γ")) ret += 3;
                return ret;
            }
        }
        public string Name { get; private set; }
        public string NameNumberLetter
        {
            get
            {
                var ret = "";
                foreach (var item in Name)
                {
                    if ("1234567890αβγ".Contains(item)) ret += item;
                }
                return ret;
            }
        }
        public string NameMonster
        {
            get
            {
                var ret = NameNumberLetter;
                if (Area < 17) return ret;
                else return $"大军团{ret}";
            }
        }

        public string EID { get; private set; }
        public string E { get; private set; }
        public string Pos { get => $"{Area}-{Row}-{Col}"; }
        public string Tag { get => $"{Type}::{Name}::{E}::{EID}"; }
        public int Type { get; set; }

        public ClubEvent(string pos, string data)
        {
            var arr = pos.Split("-");
            Area = arr.Length > 0 ? Convert.ToInt32(arr[0]) : 0;
            Row = arr.Length > 1 ? Convert.ToInt32(arr[1]) : 0;
            Col = arr.Length > 2 ? Convert.ToInt32(arr[2]) : 0;

            arr = data.Split("::");
            Type = arr.Length > 0 ? Convert.ToInt32(arr[0]) : 0;
            Name = arr.Length > 1 ? arr[1] : "";
            E = arr.Length > 2 ? arr[2] : "";
            EID = arr.Length > 3 ? arr[3] : "";
        }

        public void Update(ClubEvent other)
        {
            if (!string.IsNullOrEmpty(other.Name)) this.Name = other.Name;
            if (!string.IsNullOrEmpty(other.E)) this.E = other.E;
            if (!string.IsNullOrEmpty(other.EID)) this.EID = other.EID;

        }


    }

}
