using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RS.Snail.JJJ.Client.core.game.module.task;

namespace RS.Snail.JJJ.clone
{
    internal class Group
    {
        /// <summary> 
        /// 更新时间
        /// </summary>
        public long UpdateTime { get; set; }

        /// <summary>
        /// 群名字
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 群ID
        /// </summary>
        public string WXID { get; set; }
        public string Node { get; set; }
        /// <summary>
        /// 俱乐部ID
        /// </summary>
        public string RID { get; set; }
        public bool IsIn { get; set; }
        /// <summary>
        /// 机器人WXID
        /// </summary>

        public string RobotWXID { get; set; }

        public Dictionary<string, GroupMember> Members { get; set; } = new();


        #region PULIC METHODS
        public GroupMember? GetMemberByUID(string uid)
        {
            foreach (var member in Members)
            {
                if (member.Value.UIDs.Contains(uid))
                {
                    return member.Value;
                }
            }
            return null;
        }
        public GroupMember? GetMemberByWXID(string wxid)
        {
            if (!Members.ContainsKey(wxid)) return null;
            return Members[wxid];
        }
        public bool IsUIDBinded(string uid)
        {
            foreach (var member in Members)
            {
                if (member.Value.UIDs.Contains(uid)) return true;
            }
            return false;
        }
        public UserRole GetMemberRole(string wxid)
        {

            if (!Members.ContainsKey(wxid)) return UserRole.UNDEFINED;
            //if (Members[wxid].Role < UserRole.PLAYER)
            //{
            //    if (Const.Communicate.SpecialGroups.Contains(this.WXID))
            //        return UserRole.PLAYER;
            //}
            return Members[wxid].Role;
        }
        public bool SetMemberRole(string wxid, UserRole role)
        {
            if (!Members.ContainsKey(wxid)) return false;
            Members[wxid].Role = role;
            return true;
        }
        public string GetMemberWXIDByNick(string member_nick)
        {
            foreach (var member in Members)
            {
                if (member.Value.NickName == member_nick) return member.Key;
            }
            return "";
        }
        public string GetMemberWXIDByUID(string uid)
        {
            foreach (var member in Members)
            {
                if (member.Value.UIDs == null) continue;
                if (member.Value.UIDs.Contains(uid)) return member.Key;
            }
            return "";
        }
        public string GetMemberNameByWXID(string member_wxid)
        {
            if (!Members.ContainsKey(member_wxid)) return "";
            return Members[member_wxid].NickName;
        }
        public string[] GetMemberWXIDs(string member_nick)
        {
            var result = new List<string> { };
            foreach (var member in Members)
            {
                //if (member_nick.Contains("萌萌的"))
                //{
                //    if (member.Value.NickName == null)
                //    { Console.WriteLine(member.Key); }
                //    else
                //    { Console.WriteLine(member.Key + " - " + member.Value.NickName); }
                //}
                if (member.Value.NickName == null) continue;
                if (member.Value.NickName == member_nick || member.Value.NickName.Contains(member_nick) || member.Value.WXID == member_nick) result.Add(member.Key);
            }
            return result.ToArray();
        }
        public List<string> GetMemberWXIDs()
        {
            return Members.Keys.ToList();
        }
        public string[] GetManagerNicks()
        {
            var result = new List<string> { };
            foreach (var _item in Members)
            {
                if (_item.Value.Role == UserRole.GROUP_MANAGER) result.Add(_item.Value.NickName);
            }
            return result.ToArray();
        }
        /// <summary>
        /// 俱乐部内所有已绑定的UID
        /// </summary>
        /// <returns></returns>
        public string[] GetBindedUIDs()
        {
            var uids = new List<string> { };
            foreach (var _item in Members)
            {
                foreach (var uid in _item.Value.UIDs)
                {
                    if (!uids.Contains(uid)) uids.Add(uid);
                }
            }
            return uids.ToArray();
        }
        public List<string> GetMemberUIDs(string wxid)
        {
            if (!Members.ContainsKey(wxid)) return new List<string>();
            return Members[wxid].UIDs;
        }
        #endregion


        #region JSON
        public Group(dynamic src, string wxid)
        {
            UpdateTime = TimeHelper.ToTimeStamp();
            WXID = wxid;
            Name = JSONHelper.ParseString(src["name"]);
            Node = JSONHelper.ParseString(src["node"]);
            Members = Members ?? new();
        }
        public Group(string wxid, string name)
        {
            UpdateTime = TimeHelper.ToTimeStamp();
            this.WXID = wxid;
            this.Name = name;
            Members = Members ?? new();
        }
        public Group(dynamic src)
        {
            UpdateTime = JSONHelper.ParseLong(src["update_time"]);
            WXID = JSONHelper.ParseString(src["wxid"]);
            Name = CryptoHelper.DecryptBase64(JSONHelper.ParseString(src["name"]));
            RID = JSONHelper.ParseString(src["rid"]);
            Node = JSONHelper.ParseString(src["node"]);
            IsIn = JSONHelper.ParseBool(src["is_in"]);
            RobotWXID = JSONHelper.ParseString(src["robot_wxid"]);
            var tmp = src["members"] ?? new JObject();

            foreach (var member in tmp)
            {
                Members.Add(JSONHelper.ParseString(member.Name), new GroupMember(member.Value));
            }
        }

        /// <summary>
        /// 获取group的json
        /// </summary>
        /// <returns></returns>
        public dynamic GetJO()
        {
            //List<dynamic> member_data = new();

            dynamic member_data = new JObject();
            foreach (var member in Members)
            {
                member_data[member.Key] = member.Value.GetJO();
            }
            return JObject.FromObject(new
            {
                update_time = UpdateTime,
                wxid = WXID,
                name = CryptoHelper.EncryptBase64(Name),
                rid = RID,
                node = Node,
                is_in = IsIn,
                robot_wxid = RobotWXID,
                members = member_data,
            });
        }
        #endregion
    }
}
