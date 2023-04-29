using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.game.include;
using RS.Snail.JJJ.Client.core.res.communicate;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.modules
{
    /// <summary>
    /// 微信通讯录模块（含群/群成员 以及用户）
    /// </summary>
    internal class ContactsM : IModule
    {
        #region FIELDS
        public string ModuleName { get => "ContactsM"; }
        public Context _context { get; set; }

        private bool _inited = false;
        public bool Inited { get => _inited; }

        private ConcurrentDictionary<string, ConcurrentDictionary<string, User>> _users;
        private ConcurrentDictionary<string, ConcurrentDictionary<string, Group>> _groups;
        private ConcurrentDictionary<string, UserRole> _roleCache;
        #endregion

        #region INIT
        public ContactsM(boot.Context context)
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

        private void RegistBackups()
        {
            _context.BackupM.RegistSaveSessions(ModuleName, SaveCSV);
            _context.BackupM.RegistBackupSession(Tools.Common.Enums.CSVType.RobotData, include.files.User_List_CSV);
            _context.BackupM.RegistBackupSession(Tools.Common.Enums.CSVType.RobotData, include.files.Group_List_CSV);
            _context.BackupM.RegistBackupSession(Tools.Common.Enums.CSVType.RobotData, include.files.Role_Cache);
        }

        private void LoadCSV()
        {
            dynamic data;
            #region USERS
            try
            {
                data = IOHelper.GetCSV(Tools.Common.Enums.CSVType.RobotData, include.files.User_List_CSV);
                _users = new();
                if (data is JObject && JSONHelper.GetCount(data) > 0)
                {
                    foreach (var item in data)
                    {
                        var wechatWxid = item.Name;
                        _users.TryAdd(wechatWxid, new ConcurrentDictionary<string, User>());
                        var sub = item.Value;
                        foreach (var subItem in sub)
                        {
                            _users[wechatWxid].TryAdd(subItem.Name, new User(subItem.Value));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, $"ContactsM.LoadCSV.USERS");
            }
            #endregion

            #region GROUPS
            try
            {
                data = IOHelper.GetCSV(Tools.Common.Enums.CSVType.RobotData, include.files.Group_List_CSV);
                _groups = new();
                if (data is JObject && JSONHelper.GetCount(data) > 0)
                {
                    foreach (var item in data)
                    {
                        var wechatWxid = item.Name;
                        _groups.TryAdd(wechatWxid, new ConcurrentDictionary<string, Group>());
                        var sub = item.Value;
                        foreach (var subItem in sub)
                        {
                            _groups[wechatWxid][subItem.Name] = new Group(subItem.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, $"ContactsM.LoadCSV.GROUPS");
            }
            #endregion

            #region ROLES
            try
            {
                data = IOHelper.GetCSV(Tools.Common.Enums.CSVType.RobotData, include.files.Role_Cache) ?? new JObject();
                _roleCache = new();
                foreach (var item in data)
                {
                    _roleCache.TryAdd(item.Name, (UserRole)JSONHelper.ParseInt(item.Value));
                }
                //   _roleCache = data ?? new JObject();
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, $"ContactsM.LoadCSV.ROLES");
            }
            #endregion

        }
        private void SaveCSV()
        {
            dynamic jo;
            try
            {
                jo = new JObject();
                foreach (var item in _users)
                {
                    jo[item.Key] = jo[item.Key] ?? new JObject();
                    foreach (var item2 in item.Value)
                    {
                        jo[item.Key][item2.Key] = item2.Value.GetJO();
                    }
                }
                IOHelper.SaveCSV(RS.Tools.Common.Enums.CSVType.RobotData, jo, include.files.User_List_CSV);
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, $"ContactsM.SaveCSV.USERS");
            }

            try
            {
                jo = new JObject();
                foreach (var item in _groups)
                {
                    jo[item.Key] = jo[item.Key] ?? new JObject();
                    foreach (var item2 in item.Value)
                    {
                        jo[item.Key][item2.Key] = item2.Value.GetJO();
                    }
                }
                IOHelper.SaveCSV(RS.Tools.Common.Enums.CSVType.RobotData, jo, include.files.Group_List_CSV);
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, $"ContactsM.SaveCSV.GROUPS");
            }
            try
            {
                jo = JObject.FromObject(_roleCache);
                IOHelper.SaveCSV(RS.Tools.Common.Enums.CSVType.RobotData, jo, include.files.Role_Cache);
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, $"ContactsM.SaveCSV.ROLES");
            }

        }
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// 查询群
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="chatroom"></param>
        /// <returns></returns>
        public Group? FindGroup(string robotWxid, string chatroom)
        {
            if (string.IsNullOrEmpty(chatroom)) return null;
            if (!_groups.ContainsKey(robotWxid)) return null;
            if (!_groups[robotWxid].ContainsKey(chatroom)) return null;
            return _groups[robotWxid][chatroom];
        }

        public Group? FindGroupByRID(string robotWxid, string rid)
        {
            if (!_groups.ContainsKey(robotWxid)) return null;
            foreach (var item in _groups[robotWxid])
            {
                if (item.Value.RID == rid) return item.Value;
            }
            return null;
        }
        /// <summary>
        /// 查询通讯录好友
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public User? FindUser(string robotWxid, string wxid)
        {
            if (!_users.ContainsKey(robotWxid)) return null;
            if (!_users[robotWxid].ContainsKey(wxid)) return null;
            return _users[robotWxid][wxid];
        }
        /// <summary>
        /// 查询群成员
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="chatroom"></param>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public GroupMember? FindGroupMember(string robotWxid, string chatroom, string wxid)
        {
            if (!_groups.ContainsKey(robotWxid)) return null;
            if (!_groups[robotWxid].ContainsKey(chatroom)) return null;
            if (!_groups[robotWxid][chatroom].Members.ContainsKey(wxid)) return null;
            return _groups[robotWxid][chatroom].Members[wxid];
        }
        /// <summary>
        /// 根据群成员昵称查询群成员wxid
        /// </summary>
        /// <param name="nick"></param>
        /// <param name="robotWxid"></param>
        /// <param name="chatroom"></param>
        /// <returns></returns>
        public List<string> QueryGroupMemberWXID(string nick, string robotWxid, string chatroom)
        {
            var group = FindGroup(robotWxid, chatroom);
            if (group is null) return null;
            var ret = new List<string>();
            foreach (var item in group.Members)
            {
                if (item.Value.NickName.Contains(nick))
                {
                    ret.Add(item.Key);
                }
            }

            return ret;
        }
        /// <summary>
        /// 根据群成员wxid查询群成员昵称
        /// 如果群不存在成员，则查找通讯录
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="robotWxid"></param>
        /// <param name="chatroom"></param>
        /// <returns></returns>
        public string QueryGroupMemberNick(string wxid, string robotWxid, string chatroom)
        {
            var member = FindGroupMember(robotWxid, chatroom, wxid);
            if (member is not null) return member.NickName;

            var user = FindUser(robotWxid, wxid);
            if (user is not null) return user.NickName;
            return "";
        }
        /// <summary>
        /// 判断字符串是否是本群内的成员的wxid
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="robotWxid"></param>
        /// <param name="chatroom"></param>
        /// <returns></returns>
        public bool IsGroupMemberWXID(string wxid, string robotWxid, string chatroom)
        {
            return FindGroupMember(robotWxid, chatroom, wxid) is not null;
        }
        /// <summary>
        /// 通过rid查找现有会长
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryClubHolder(string robotWxid, string rid)
        {
            if (!_users.ContainsKey(robotWxid)) return "";
            foreach (var user in _users[robotWxid])
            {
                if (user.Value.HoldsRIDs is not null && user.Value.HoldsRIDs.Contains(rid)) return user.Key;
            }

            return "";
        }

        #region SET VALUES
        /// <summary>
        /// 设置会长
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="rid"></param>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public bool SetHolder(string robotWxid, string rid, string wxid)
        {
            if (!_users.ContainsKey(robotWxid)) return false;
            foreach (var user in _users[robotWxid])
            {
                if (user.Key == wxid)
                {
                    user.Value.HoldsRIDs = user.Value.HoldsRIDs ?? new List<string>();
                    if (!user.Value.HoldsRIDs.Contains(rid)) user.Value.HoldsRIDs.Add(rid);
                    user.Value.Role = UserRole.GROUP_HOLDER;
                    continue;
                }

                // 做一个修复，去重
                if (user.Value.HoldsRIDs is not null && user.Value.HoldsRIDs.Contains(rid))
                {
                    user.Value.HoldsRIDs.Remove(rid);
                    if (user.Value.HoldsRIDs.Count <= 0) user.Value.Role = UserRole.NONE;
                }
            }


            return true;
        }
        /// <summary>
        /// 移除会长
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="rid"></param>
        /// <returns></returns>
        public bool DelHolder(string robotWxid, string rid)
        {
            if (!_users.ContainsKey(robotWxid)) return false;
            foreach (var user in _users[robotWxid])
            {
                if (user.Value.HoldsRIDs is not null && user.Value.HoldsRIDs.Contains(rid))
                {
                    user.Value.HoldsRIDs.Remove(rid);
                    if (user.Value.HoldsRIDs.Count <= 0) user.Value.Role = UserRole.NONE;
                }
            }

            return true;
        }
        /// <summary>
        /// 绑定群
        /// 不含迁移
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="chatroom"></param>
        /// <param name="rid"></param>
        /// <returns></returns>
        public bool BindGroup(string robotWxid, string chatroom, string rid)
        {
            if (!_groups.ContainsKey(robotWxid)) return false;
            foreach (var group in _groups[robotWxid])
            {
                if (group.Key == chatroom) group.Value.RID = rid;
                else if (group.Value.RID == rid) group.Value.RID = "";
            }
            return true;
        }
        /// <summary>
        /// 解绑群
        /// 解绑后成员绑定和权限将被清空
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="chatroom"></param>
        /// <returns></returns>
        public bool UnbindGroup(string robotWxid, string chatroom)
        {
            if (!_groups.ContainsKey(robotWxid)) return false;
            var group = FindGroup(robotWxid, chatroom);
            if (group is null) return false;
            group.RID = "";
            foreach (var member in group.Members)
            {
                member.Value.UIDs = new List<string>();
                member.Value.Role = include.UserRole.NONE;
            }
            ClearGroupMemberRoleChache(robotWxid, chatroom);
            return true;
        }
        /// <summary>
        /// 迁移俱乐部绑定
        /// 迁移后原微信群成员绑定和权限将被清空
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="oriChatroom"></param>
        /// <param name="newChatroom"></param>
        /// <param name="rid"></param>
        /// <returns></returns>
        public bool MoveBindGroup(string robotWxid, string oriChatroom, string newChatroom, string rid)
        {
            if (!_groups.ContainsKey(robotWxid)) return false;
            var oriGroup = FindGroup(robotWxid, oriChatroom);
            var newGroup = FindGroup(robotWxid, newChatroom);
            if (oriChatroom is null || newGroup is null) return false;

            if (!BindGroup(robotWxid, newChatroom, rid)) return false;

            foreach (var oriMember in oriGroup.Members)
            {
                if (newGroup.Members.ContainsKey(oriMember.Key))
                {
                    newGroup.Members[oriMember.Key].UIDs = oriMember.Value.UIDs;
                    newGroup.Members[oriMember.Key].Role = oriMember.Value.Role;
                }
                oriMember.Value.UIDs = new List<string>();
                oriMember.Value.Role = include.UserRole.NONE;
            }
            ClearGroupMemberRoleChache(robotWxid, oriChatroom);

            return true;
        }
        /// <summary>
        /// 设置经理
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="chatroom"></param>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public bool SetManager(string robotWxid, string chatroom, string wxid)
        {
            var group = FindGroup(robotWxid, chatroom);
            if (group is null) return false;

            if (!group.Members.ContainsKey(wxid)) return false;

            group.Members[wxid].Role = include.UserRole.GROUP_MANAGER;
            SetRoleCache(UserRole.GROUP_MANAGER, wxid, robotWxid, chatroom);

            return true;
        }
        /// <summary>
        /// 移除经理
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="chatroom"></param>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public bool DelManager(string robotWxid, string chatroom, string wxid)
        {
            var group = FindGroup(robotWxid, chatroom);
            if (group is null) return false;

            if (!group.Members.ContainsKey(wxid)) return false;

            var curRole = group.Members[wxid].UIDs.Count > 0 ? UserRole.NORMAL : UserRole.NONE;
            group.Members[wxid].Role = curRole;
            SetRoleCache(curRole, wxid, robotWxid, chatroom);
            return true;
        }
        /// <summary>
        /// 设置成员
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="chatroom"></param>
        /// <param name="wxid"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        public bool SetMember(string robotWxid, string chatroom, string wxid, string uid)
        {
            var group = FindGroup(robotWxid, chatroom);
            if (group is null) return false;

            if (!group.Members.ContainsKey(wxid)) return false;

            group.Members[wxid].UIDs = group.Members[wxid].UIDs ?? new();
            if (!group.Members[wxid].UIDs.Contains(uid)) group.Members[wxid].UIDs.Add(uid);

            var role = group.Members[wxid].Role;
            if (role < UserRole.NORMAL) role = UserRole.NORMAL;
            group.Members[wxid].Role = role;
            SetRoleCache(role, wxid, robotWxid, chatroom);

            return true;
        }
        /// <summary>
        /// 移除成员
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="chatroom"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        public bool DelMember(string robotWxid, string chatroom, string uid)
        {
            var group = FindGroup(robotWxid, chatroom);
            if (group is null) return false;

            foreach (var member in group.Members)
            {
                if (member.Value.UIDs is not null && member.Value.UIDs.Contains(uid))
                {
                    member.Value.UIDs.Remove(uid);
                    if (member.Value.UIDs.Count <= 0)
                    {
                        var role = member.Value.Role;
                        if (role <= UserRole.NORMAL)
                        {
                            role = UserRole.NONE;
                            member.Value.Role = role;
                            SetRoleCache(role, member.Key, robotWxid, chatroom);
                        }
                    }
                }
            }

            return true;
        }
        /// <summary>
        /// 刷新微信群成员昵称
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool RefreshGroupMemberNicks(string robotWxid, Dictionary<string, List<(string wxid, string displayName, string nickName)>> data)
        {
            if (!_users.ContainsKey(robotWxid)) _users.TryAdd(robotWxid, new ConcurrentDictionary<string, User>());
            if (!_groups.ContainsKey(robotWxid)) _groups.TryAdd(robotWxid, new ConcurrentDictionary<string, Group>());
            foreach (var groupData in data)
            {
                if (!_groups[robotWxid].ContainsKey(groupData.Key)) _groups[robotWxid].TryAdd(groupData.Key, new Group(groupData.Key, ""));
                _groups[robotWxid][groupData.Key].IsIn = true;

                foreach (var memberData in groupData.Value)
                {
                    if (_groups[robotWxid][groupData.Key].Members.ContainsKey(memberData.wxid))
                    {
                        _groups[robotWxid][groupData.Key].Members[memberData.wxid].NickName = memberData.displayName;
                        _groups[robotWxid][groupData.Key].Members[memberData.wxid].IsInGroup = true;
                    }
                    else
                    {
                        _groups[robotWxid][groupData.Key].Members.TryAdd(memberData.wxid, new GroupMember(memberData.wxid, memberData.displayName));
                    }

                    // 加入user列表
                    if (_users[robotWxid].ContainsKey(memberData.wxid)) _users[robotWxid][memberData.wxid].NickName = memberData.nickName;
                    else _users[robotWxid].TryAdd(memberData.wxid, new User(memberData.wxid, memberData.nickName) { IsFriend = false });
                }

                // 更新不存在的成员
                var exitList = new List<string>();
                var existWxids = groupData.Value.Select((a) => a.wxid).ToList();
                foreach (var item in _groups[robotWxid][groupData.Key].Members)
                {
                    if (!item.Value.IsInGroup) continue;
                    if (!existWxids.Contains(item.Key))
                    {
                        item.Value.IsInGroup = false;
                        exitList.Add(item.Key);
                    }
                }

                // 提醒退群
                if (exitList.Count > 0 && StringHelper.IsRID(_groups[robotWxid][groupData.Key].RID))
                {
                    var club = _context.ClubsM.FindClub(robotWxid, _groups[robotWxid][groupData.Key].RID);
                    if (club is not null && !club.DontRemindGroupMemberChanged)
                    {
                        var msg = $"发现 {existWxids.Count} 人退群" +
                            string.Join("\n", exitList.Select(a => QueryGroupMemberNick(a, robotWxid, groupData.Key)));
                        var holder = QueryClubHolder(robotWxid, club.RID);
                        if (club.LoginDontAtHolder || string.IsNullOrEmpty(holder))
                        {
                            _context.WechatM.SendText(msg, robotWxid, groupData.Key);
                        }
                        else
                        {
                            _context.WechatM.SendAtText(msg, new List<string> { holder }, robotWxid, groupData.Key);
                        }
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// 刷新联系人信息
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool RefreshContactList(string robotWxid, dynamic data)
        {
            if (data is not JArray) return false;
            if (!_users.ContainsKey(robotWxid)) _users.TryAdd(robotWxid, new ConcurrentDictionary<string, User>());
            if (!_groups.ContainsKey(robotWxid)) _groups.TryAdd(robotWxid, new ConcurrentDictionary<string, Group>());

            var friends = new List<string>();
            var groups = new List<string>();
            foreach (var contact in data)
            {
                string id = JSONHelper.ParseString(contact.wxid);
                var isGroup = id.EndsWith("@chatroom");
                if (!isGroup && id.Contains("@")) continue;

                var nick = JSONHelper.ParseString(contact.wxNickName);
                var remark = JSONHelper.ParseString(contact.wxRemark);
                // 更新群信息
                if (isGroup)
                {
                    groups.Add(id);
                    if (!_groups[robotWxid].ContainsKey(id)) _groups[robotWxid].TryAdd(id, new Group(id, nick));
                    else _groups[robotWxid][id].Name = nick;
                    _groups[robotWxid][id].IsIn = true;
                }
                // 更新联系人信息
                else
                {
                    friends.Add(id);
                    if (!_users[robotWxid].ContainsKey(id)) _users[robotWxid].TryAdd(id, new User(id, nick));
                    else _users[robotWxid][id].NickName = nick;
                    _users[robotWxid][id].Remark = remark;
                    _users[robotWxid][id].IsFriend = true;
                }
            }

            // 检查不存在的联系人
            foreach (var group in _groups[robotWxid])
            {
                if (!groups.Contains(group.Key)) group.Value.IsIn = false;
            }
            foreach (var user in _users[robotWxid])
            {
                if (!friends.Contains(user.Key)) user.Value.IsFriend = false;
            }

            return true;
        }
        /// <summary>
        /// 刷新机器人信息
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool RefreshSelfInfo(string robotWxid, dynamic data)
        {
            if (data is not JObject) return false;
            if (!_users.ContainsKey(robotWxid)) _users.TryAdd(robotWxid, new ConcurrentDictionary<string, User>());

            string path = JSONHelper.ParseString(data.wxFilePath);
            var arr = path.Split("\\", StringSplitOptions.RemoveEmptyEntries);
            arr = arr[..^1];
            include.files.WechatFileRoot = string.Join("\\", arr) + "\\";

            var nick = JSONHelper.ParseString(data.wxNickName);
            if (!_users[robotWxid].ContainsKey(robotWxid)) _users[robotWxid].TryAdd(robotWxid, new(robotWxid, nick));
            else _users[robotWxid][robotWxid].NickName = nick;

            return true;
        }
        /// <summary>
        /// 所有群的wxid
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="onlyClubBinded">是否只包含绑定俱乐部的群</param>
        /// <returns></returns>
        public List<string> GetAllGroupWXID(string robotWxid, bool onlyClubBinded = false)
        {
            var ret = new List<string>();
            if (!_groups.ContainsKey(robotWxid)) return ret;
            foreach (var item in _groups[robotWxid])
            {
                if (!item.Value.IsIn) continue;
                if (onlyClubBinded && string.IsNullOrEmpty(item.Value.RID)) continue;
                ret.Add(item.Key);
            }

            return ret;
        }
        #endregion

        #region ROLE
        /// <summary>
        /// 查找权限
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="chatroom"></param>
        /// <param name="chatroom"></param>
        /// <returns></returns>
        public UserRole QueryRole(string robotWxid, string wxid, string chatroom = "")
        {
            // admin
            var wechatCFG = _context.WechatM.FindWechatCFG(robotWxid);
            if (wechatCFG is not null && wechatCFG.Admins.Contains(wxid)) return UserRole.ADMINISTRATOR;

            // holders
            if (_users.ContainsKey(robotWxid))
            {
                if (_users[robotWxid].ContainsKey(wxid))
                {
                    if (_users[robotWxid][wxid].Role >= UserRole.GROUP_HOLDER) return _users[robotWxid][wxid].Role;
                }
            }

            var role = QueryRoleInternal(wxid, robotWxid, chatroom);
            if (role != UserRole.UNDEFINED) return role;
            else role = CalcRole(wxid, robotWxid, chatroom);
            SetRoleCache(role, wxid, robotWxid, chatroom);
            return role;
        }
        /// <summary>
        /// 检查具有的权限是否在本群和本俱乐部有效
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="rid">俱乐部rid</param>
        /// <param name="wxid">成员WXID</param>
        /// <param name="chatroom">群WXID, 私聊为空</param>
        /// <returns></returns>
        public bool CheckGroupRole(string robotWxid, string rid, string wxid, string chatroom = "")
        {

            var role = QueryRole(robotWxid, wxid, chatroom);
            if (role == UserRole.ADMINISTRATOR) return true;
            else if (role == UserRole.GROUP_HOLDER)
            {
                var user = FindUser(robotWxid, wxid);
                if (user is null) return false;
                if (!user.HoldsRIDs.Contains(rid)) return false;
                return true;
            }
            else if (role == UserRole.GROUP_MANAGER || role == UserRole.NORMAL)
            {
                if (string.IsNullOrEmpty(chatroom))
                {
                    var group = FindGroupByRID(robotWxid, rid);
                    if (group is null) return false;
                    chatroom = group.WXID;
                }
                var user = FindGroupMember(robotWxid, chatroom, wxid);
                if (user is null) return false;
                return user.Role >= role;
            }
            return false;
        }

        private UserRole QueryRoleInternal(string wxid, string robotWxid, string chatroom = "")
        {
            var key = GetRoleCacheKey(wxid, robotWxid, chatroom);
            return _roleCache.ContainsKey(key) ? _roleCache[key] : UserRole.UNDEFINED;
        }
        /// <summary>
        /// 设置权限缓存
        /// </summary>
        /// <param name="role"></param>
        /// <param name="wxid"></param>
        /// <param name="robotWxid"></param>
        /// <param name="chatroom"></param>
        public void SetRoleCache(UserRole role, string wxid, string robotWxid, string chatroom = "")
        {
            var key = GetRoleCacheKey(wxid, robotWxid, chatroom);
            if (!_roleCache.ContainsKey(key)) _roleCache.TryAdd(key, role);
            else _roleCache[key] = role;
        }

        private string GetRoleCacheKey(string wxid, string robotWxid, string chatroom = "")
        {
            return $"{wxid}-{robotWxid}-{chatroom}";
        }
        /// <summary>
        /// 计算某个wxid的权限
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="robotWxid">机器人wxid</param>
        /// <param name="chatroom">群id，若为空，则检查所有群的最大权限</param>
        /// <returns></returns>
        private UserRole CalcRole(string wxid, string robotWxid, string chatroom = "")
        {
            // 指定群，寻找指定群内的成员权限
            if (!string.IsNullOrEmpty(chatroom))
            {
                if (_groups.ContainsKey(robotWxid))
                {
                    if (_groups[robotWxid].ContainsKey(chatroom))
                    {
                        var r = _groups[robotWxid][chatroom].GetMemberRole(wxid);
                        if (r > UserRole.UNDEFINED) return r;
                    }
                }

                return UserRole.UNDEFINED;
            }
            // 未指定群，查询所有群内最高权限
            var role = UserRole.UNDEFINED;
            if (_groups.ContainsKey(robotWxid))
            {
                foreach (var item in _groups[robotWxid])
                {
                    if (item.Value.Members.ContainsKey(wxid))
                    {
                        role = item.Value.Members[wxid].Role > role ? item.Value.Members[wxid].Role : role;
                    }
                }
            }

            return role;
        }
        public void ClearGroupMemberRoleChache(string robotWxid, string chatroom)
        {
            var group = FindGroup(robotWxid, chatroom);
            if (group is null) return;

            foreach (var member in group.Members)
            {
                SetRoleCache(UserRole.NONE, robotWxid, chatroom, member.Key);
            }
        }
        #endregion

        #endregion
    }
}
