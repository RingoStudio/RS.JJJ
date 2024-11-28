using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.game.include;
using RS.Snail.JJJ.Client.core.res.communicate;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Utils;
using RS.Tools.Excel;
using RS.WechatFerry.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        private ConcurrentDictionary<string, User> _users;
        private ConcurrentDictionary<string, Group> _groups;
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
                        _users.TryAdd(item.Name, new User(item.Value));
                    }
                }
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, $"ContactsM.LoadCSV.USERS");
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

                        _groups[item.Name] = new Group(item.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, $"ContactsM.LoadCSV.GROUPS");
            }
            #endregion

            #region ROLES
            try
            {
                //data = IOHelper.GetCSV(Tools.Common.Enums.CSVType.RobotData, include.files.Role_Cache) ?? new JObject();
                _roleCache = new();
                //foreach (var item in data)
                //{
                //    _roleCache.TryAdd(item.Name, (UserRole)JSONHelper.ParseInt(item.Value));
                //}
                //   _roleCache = data ?? new JObject();
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, $"ContactsM.LoadCSV.ROLES");
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
                    jo[item.Key] = item.Value.GetJO();
                }
                IOHelper.SaveCSV(RS.Tools.Common.Enums.CSVType.RobotData, jo, include.files.User_List_CSV);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, $"ContactsM.SaveCSV.USERS");
            }

            try
            {
                jo = new JObject();
                foreach (var item in _groups)
                {
                    jo[item.Key] = item.Value.GetJO();
                }
                IOHelper.SaveCSV(RS.Tools.Common.Enums.CSVType.RobotData, jo, include.files.Group_List_CSV);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, $"ContactsM.SaveCSV.GROUPS");
            }
            try
            {
                jo = JObject.FromObject(_roleCache);
                IOHelper.SaveCSV(RS.Tools.Common.Enums.CSVType.RobotData, jo, include.files.Role_Cache);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, $"ContactsM.SaveCSV.ROLES");
            }

        }
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// 查询群
        /// </summary>
        /// <param name="chatroom"></param>
        /// <returns></returns>
        public Group? FindGroup(string chatroom)
        {
            if (string.IsNullOrEmpty(chatroom)) return null;
            if (!_groups.ContainsKey(chatroom)) return null;
            return _groups[chatroom];
        }
        /// <summary>
        /// 通过俱乐部RID查找绑定群
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public Group? FindGroupByRID(string rid)
        {
            foreach (var item in _groups)
            {
                if (item.Value.RID == rid) return item.Value;
            }
            return null;
        }
        /// <summary>
        /// 查询通讯录好友
        /// </summary>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public User? FindUser(string wxid)
        {
            if (!_users.ContainsKey(wxid)) return null;
            return _users[wxid];
        }
        /// <summary>
        /// 通过昵称寻找用户
        /// </summary>
        /// <param name="nick"></param>
        /// <returns></returns>
        public List<User> FindUserByNick(string nick)
        {
            var ret = new List<User>();
            foreach (var item in _users)
            {
                if (item.Value.NickName == nick || item.Value.Alias == nick) ret.Add(item.Value);
            }
            return ret;
        }
        /// <summary>
        /// 查询群成员
        /// </summary>
        /// <param name="chatroom"></param>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public GroupMember? FindGroupMember(string chatroom, string wxid)
        {
            if (!_groups.ContainsKey(chatroom)) return null;
            if (!_groups[chatroom].Members.ContainsKey(wxid)) return null;
            return _groups[chatroom].Members[wxid];
        }
        /// <summary>
        /// 根据群成员昵称查询群成员wxid
        /// </summary>
        /// <param name="nick"></param>
        /// <param name="chatroom"></param>
        /// <returns></returns>
        public List<string> QueryGroupMemberWXID(string nick, string chatroom)
        {
            var ret = new List<string>();
            var group = FindGroup(chatroom);
            if (group is null) return ret;
            foreach (var item in group.Members)
            {
                if (item.Value.NickName.Contains(nick))
                {
                    ret.Add(item.Key);
                }
            }
            var users = _context.ContactsM.FindUserByNick(nick);
            if (users.Count > 0)
            {
                foreach (var user in users)
                {
                    if ((!ret.Contains(user.WXID)) && IsGroupMemberWXID(user.WXID, chatroom))
                        ret.Add(user.WXID);
                }
            }
            return ret;
        }
        /// <summary>
        /// 通过WXID查询用户昵称
        /// </summary>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public string QueryContactNick(string wxid)
        {
            if (wxid.EndsWith("@chatroom"))
            {
                var group = FindGroup(wxid);
                if (group is not null) return group.Name;
                return $"微信群({wxid})";
            }
            else
            {
                var member = FindUser(wxid);
                if (member is not null) return member.NickName;
                return $"好友({wxid})";
            }
        }
        /// <summary>
        /// 根据群成员wxid查询群成员昵称
        /// 如果群不存在成员，则查找通讯录
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="chatroom"></param>
        /// <returns></returns>
        public string QueryGroupMemberNick(string wxid, string chatroom)
        {
            var member = FindGroupMember(chatroom, wxid);
            if (member is not null) return member.NickName;

            var user = FindUser(wxid);
            if (user is not null) return user.NickName;
            return "";
        }
        /// <summary>
        /// 查找群成员昵称，用于文件名（过滤非法字符）
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="chatroom"></param>
        /// <returns></returns>
        public string QueryGroupMemberNickForFile(string wxid, string chatroom)
        {
            var nick = QueryGroupMemberNick(wxid, chatroom);
            if (string.IsNullOrEmpty(nick)) return "";
            nick = utils.Utils.GetNickNameForFileName(nick);
            return nick;
        }
        /// <summary>
        /// 判断字符串是否是本群内的成员的wxid
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="chatroom"></param>
        /// <returns></returns>
        public bool IsGroupMemberWXID(string wxid, string chatroom) => FindGroupMember(chatroom, wxid) is not null;
        /// <summary>
        /// 通过rid查找现有会长WXID
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryClubHolderWXID(string rid)
        {
            foreach (var user in _users)
            {
                if (user.Value.HoldsRIDs is not null && user.Value.HoldsRIDs.Contains(rid)) return user.Key;
            }
            return "";
        }
        /// <summary>
        /// 查询（俱乐部）微信群总览
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public string QueryGroupChatroomExcel(string rid, bool isPrivate)
        {
            try
            {

                var group = FindGroupByRID(rid);
                if (group is null) return null;

                var fileName = $"OUT\\{ExcelHelper.GetFileName($"{include.files.File_ClubChatroom}_{group.Name}_{rid}")}";
                var content = new List<List<string>>
            {
                new(){
                    "RID",
                    "俱乐部",
                    "微信群ID",
                    "微信群名称",
                    "成员WXID",
                    "成员群昵称",
                    "成员微信名",
                    "更新时间",
                    "绑定角色",
                    "群内权限",
                }
            };
                var club = _context.ClubsM.FindClub(rid) ?? new Club(_context, rid, Tools.Common.Enums.ChannelType.ANDROID);
                foreach (var member in group.Members)
                {

                    var user = FindUser(member.Value.WXID);
                    var binds = member.Value.UIDs.Select(x => $"{_context.ClubsM.QueryMemberName(x)}[{x}]");

                    content.Add(new()
                {
                    rid,
                    club.Name,
                    isPrivate? group.WXID:"****",
                    group.Name,
                    isPrivate? member.Value.WXID:"****",
                    member.Value.NickName,
                    user ?.NickName??"",
                    TimeHelper.DateTimeDesc(member.Value.UpdateTime, "yyyy-MM-dd HH:mm:ss"),
                    (binds is null || binds.Count()==0)? "[red]无绑定":string.Join("\n", binds),
                    include.wechat.UserRoleDesc(QueryRole(member.Value.WXID, group.WXID )),
                });
                }
                return ExcelHelper.SaveSingleWorksheet(content, fileName, $"{group.Name}微信群总览");

            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "ContactsM.QueryGroupChatroomExcel");
                return "";
            }
        }

        #region SET VALUES
        /// <summary>
        /// 设置会长
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public bool SetHolder(string rid, string wxid)
        {
            foreach (var user in _users)
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

            ClearUserRoleCache(wxid);
            return true;
        }
        /// <summary>
        /// 移除会长
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public bool DelHolder(string rid)
        {
            foreach (var user in _users)
            {
                if (user.Value.HoldsRIDs is not null && user.Value.HoldsRIDs.Contains(rid))
                {
                    user.Value.HoldsRIDs.Remove(rid);
                    if (user.Value.HoldsRIDs.Count <= 0)
                    {
                        user.Value.Role = UserRole.NONE;

                    }
                    ClearUserRoleCache(user.Value.WXID);
                }
            }

            return true;
        }
        /// <summary>
        /// 绑定群
        /// 不含迁移
        /// </summary>
        /// <param name="chatroom"></param>
        /// <param name="rid"></param>
        /// <returns></returns>
        public bool BindGroup(string chatroom, string rid)
        {
            foreach (var group in _groups)
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
        /// <param name="chatroom"></param>
        /// <returns></returns>
        public bool UnbindGroup(string chatroom)
        {
            var group = FindGroup(chatroom);
            if (group is null) return false;
            group.RID = "";
            foreach (var member in group.Members)
            {
                member.Value.UIDs = new List<string>();
                member.Value.Role = include.UserRole.NONE;
            }
            ClearGroupMemberRoleCache(chatroom);
            return true;
        }
        /// <summary>
        /// 迁移俱乐部绑定
        /// 迁移后原微信群成员绑定和权限将被清空
        /// </summary>
        /// <param name="oriChatroom"></param>
        /// <param name="newChatroom"></param>
        /// <param name="rid"></param>
        /// <returns></returns>
        public bool MoveBindGroup(string oriChatroom, string newChatroom, string rid)
        {
            var oriGroup = FindGroup(oriChatroom);
            var newGroup = FindGroup(newChatroom);
            if (oriChatroom is null || newGroup is null) return false;

            if (!BindGroup(newChatroom, rid)) return false;

            foreach (var oriMember in oriGroup?.Members ?? new())
            {
                if (newGroup.Members.ContainsKey(oriMember.Key))
                {
                    newGroup.Members[oriMember.Key].UIDs = oriMember.Value.UIDs;
                    newGroup.Members[oriMember.Key].Role = oriMember.Value.Role;
                }
                oriMember.Value.UIDs = new List<string>();
                oriMember.Value.Role = include.UserRole.NONE;
            }
            ClearGroupMemberRoleCache(oriChatroom);

            return true;
        }
        /// <summary>
        /// 设置经理
        /// </summary>
        /// <param name="chatroom"></param>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public bool SetManager(string chatroom, string wxid)
        {
            var group = FindGroup(chatroom);
            if (group is null) return false;

            if (!group.Members.ContainsKey(wxid)) return false;

            group.Members[wxid].Role = (UserRole)Math.Max((int)group.Members[wxid].Role, (int)include.UserRole.GROUP_MANAGER);
            ClearUserRoleCache(wxid);

            return true;
        }
        /// <summary>
        /// 移除经理
        /// </summary>
        /// <param name="chatroom"></param>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public bool DelManager(string chatroom, string wxid)
        {
            var group = FindGroup(chatroom);
            if (group is null) return false;

            if (!group.Members.ContainsKey(wxid)) return false;

            var curRole = group.Members[wxid].UIDs.Count > 0 ? UserRole.NORMAL : UserRole.NONE;
            group.Members[wxid].Role = curRole;

            ClearUserRoleCache(wxid);
            return true;
        }
        /// <summary>
        /// 设置成员
        /// </summary>
        /// <param name="chatroom"></param>
        /// <param name="wxid"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        public bool SetMember(string chatroom, string wxid, string uid)
        {
            var group = FindGroup(chatroom);
            if (group is null) return false;

            if (!group.Members.ContainsKey(wxid)) return false;

            group.Members[wxid].UIDs = group.Members[wxid].UIDs ?? new();
            if (!group.Members[wxid].UIDs.Contains(uid)) group.Members[wxid].UIDs.Add(uid);

            var role = group.Members[wxid].Role;
            if (role < UserRole.NORMAL) role = UserRole.NORMAL;
            group.Members[wxid].Role = role;

            ClearUserRoleCache(wxid);
            return true;
        }
        /// <summary>
        /// 移除成员
        /// </summary>
        /// <param name="chatroom"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        public bool DelMember(string chatroom, string uid)
        {
            var group = FindGroup(chatroom);
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
                            SetRoleCache(role, member.Key, chatroom);
                        }
                    }
                    ClearUserRoleCache(member.Value.WXID);
                }
            }

            return true;
        }
        /// <summary>
        /// 刷新微信群成员昵称
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool RefreshGroupMemberNicks(Dictionary<string, Dictionary<string, string>> data)
        {
            foreach (var groupData in data)
            {
                if (!_groups.ContainsKey(groupData.Key)) _groups.TryAdd(groupData.Key, new Group(groupData.Key, ""));
                _groups[groupData.Key].IsIn = true;

                foreach (var memberData in groupData.Value)
                {
                    if (_groups[groupData.Key].Members.ContainsKey(memberData.Key))
                    {
                        _groups[groupData.Key].Members[memberData.Key].NickName = memberData.Value;
                        _groups[groupData.Key].Members[memberData.Key].IsInGroup = true;
                    }
                    else
                    {
                        _groups[groupData.Key].Members.TryAdd(memberData.Key, new GroupMember(memberData.Key, memberData.Value));
                    }

                    // 加入user列表
                    if (_users.ContainsKey(memberData.Key)) _users[memberData.Key].NickName = memberData.Value;
                    else _users.TryAdd(memberData.Key, new User(memberData.Key, memberData.Value) { IsFriend = false });
                }

                // 更新不存在的成员
                var exitList = new List<string>();
                var existWxids = groupData.Value.Select((a) => a.Key).ToList();
                foreach (var item in _groups[groupData.Key].Members)
                {
                    if (!item.Value.IsInGroup) continue;
                    if (!existWxids.Contains(item.Key))
                    {
                        item.Value.IsInGroup = false;
                        exitList.Add(item.Key);
                    }
                }

                // 提醒退群
                if (exitList.Count > 0 && StringHelper.IsRID(_groups[groupData.Key].RID))
                {
                    var club = _context.ClubsM.FindClub(_groups[groupData.Key].RID);
                    if (club is not null && !club.DontRemindGroupMemberChanged)
                    {
                        var msg = $"发现 {existWxids.Count} 人退群" +
                            string.Join("\n", exitList.Select(a => QueryGroupMemberNick(a, groupData.Key)));
                        var holder = QueryClubHolderWXID(club.RID);
                        if (club.LoginDontAtHolder || string.IsNullOrEmpty(holder))
                        {
                            //_context.WechatM.SendText(msg, groupData.Key);
                        }
                        else
                        {
                            //_context.WechatM.SendAtText(msg, new List<string> { holder }, groupData.Key);
                        }
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// 刷新联系人信息
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool RefreshContactList(List<Contact>? data)
        {
            if (data is null || data.Count <= 0) return false;
            var friends = new List<string>();
            var groups = new List<string>();
            foreach (var contact in data)
            {
                string id = contact.Wxid ?? "";
                if (string.IsNullOrEmpty(id)) continue;
                var isGroup = id.EndsWith("@chatroom");
                if (!isGroup && id.Contains("@")) continue;

                var nick = contact.Name;
                var remark = contact.Remark;
                // 更新群信息
                if (isGroup)
                {
                    groups.Add(id);
                    if (!_groups.ContainsKey(id)) _groups.TryAdd(id, new Group(id, nick));
                    else _groups[id].Name = nick;
                    _groups[id].IsIn = true;
                }
                // 更新联系人信息
                else
                {
                    friends.Add(id);
                    if (!_users.ContainsKey(id)) _users.TryAdd(id, new User(id, nick));
                    else _users[id].NickName = nick;
                    _users[id].Remark = remark;
                    _users[id].IsFriend = true;
                    _users[id].Country = contact.Country ?? "";
                    _users[id].Province = contact.Province ?? "";
                    _users[id].City = contact.City ?? "";
                    _users[id].Gender = contact.Gender;
                }
            }

            // 检查不存在的联系人
            foreach (var group in _groups)
            {
                if (!groups.Contains(group.Key)) group.Value.IsIn = false;
            }
            foreach (var user in _users)
            {
                if (!friends.Contains(user.Key)) user.Value.IsFriend = false;
            }

            return true;
        }
        /// <summary>
        /// 刷新机器人信息
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool RefreshSelfInfo(UserInfo data)
        {
            if (data is null || string.IsNullOrEmpty(data.Wxid)) return false;
            _users[data.Wxid] = new User(data);
            return true;
        }
        /// <summary>
        /// 所有群的wxid
        /// </summary>
        /// <param name="onlyClubBinded">是否只包含绑定俱乐部的群</param>
        /// <returns></returns>
        public List<string> GetAllGroupWXID(bool onlyClubBinded = false)
        {
            var ret = new List<string>();
            foreach (var item in _groups)
            {
                if (!item.Value.IsIn) continue;
                if (onlyClubBinded && string.IsNullOrEmpty(item.Value.RID)) continue;
                ret.Add(item.Key);
            }

            return ret;
        }
        #endregion

        #region ROLE
        public bool IsAdmin(string wxid)
        {
            var config = _context.WechatM.WechatCFG();
            return config is not null && config.Admins.Contains(wxid);
        }
        public bool IsMaydayGroup(string chatroom) => _context.MaydayM.IsMaydayGroup(chatroom);
        public bool IsPlayerGroup(string chatroom) => _context.WechatM.VisitorGroups().Contains(chatroom);
        /// <summary>
        /// 查找权限
        /// 
        /// </summary>
        /// <param name="chatroom"></param>
        /// <param name="chatroom"></param>
        /// <returns></returns>
        public UserRole QueryRole(string wxid, string chatroom = "", string rid = "")
        {
            var role = QueryRoleInternal(wxid, chatroom, rid);
            if (role <= UserRole.NONE) role = CalcRole(wxid, chatroom, rid);
            SetRoleCache(role, wxid, chatroom, rid);
            return role;
        }
        /// <summary>
        /// 检查具有的权限是否在本群和本俱乐部有效
        /// </summary>
        /// <param name="rid">俱乐部rid</param>
        /// <param name="wxid">成员WXID</param>
        /// <param name="roomID">群WXID, 私聊为空</param>
        /// <returns></returns>
        //public UserRole QueryGroupRole(string rid, string wxid, string roomID)
        //{
        //    if (roomID == wxid) roomID = "";

        //    var wechatCFG = _context.WechatM.WechatCFG();
        //    if (wechatCFG is not null && wechatCFG.Admins.Contains(wxid)) return UserRole.ADMINISTRATOR;


        //    else if (role == UserRole.GROUP_HOLDER)
        //    {
        //        var user = FindUser(wxid);
        //        if (user is null) return false;
        //        if (!user.HoldsRIDs.Contains(rid)) return false;
        //        return true;
        //    }
        //    else if (role == UserRole.GROUP_MANAGER || role == UserRole.NORMAL)
        //    {
        //        if (string.IsNullOrEmpty(roomID))
        //        {
        //            var group = FindGroupByRID(rid);
        //            if (group is null) return false;
        //            roomID = group.WXID;
        //        }
        //        var user = FindGroupMember(roomID, wxid);
        //        if (user is null) return false;
        //        return user.Role >= role;
        //    }
        //    return false;
        //}

        private UserRole QueryRoleInternal(string wxid, string chatroom = "", string rid = "")
        {
            var key = GetRoleCacheKey(wxid, chatroom, rid);
            return _roleCache.ContainsKey(key) ? _roleCache[key] : UserRole.UNDEFINED;
        }
        /// <summary>
        /// 设置权限缓存
        /// </summary>
        /// <param name="role"></param>
        /// <param name="wxid"></param>
        /// <param name="chatroom"></param>
        public void SetRoleCache(UserRole role, string wxid, string chatroom = "", string rid = "")
        {
            var key = GetRoleCacheKey(wxid, chatroom, rid);
            if (!_roleCache.ContainsKey(key)) _roleCache.TryAdd(key, role);
            else _roleCache[key] = role;
        }

        private string GetRoleCacheKey(string wxid, string chatroom = "", string rid = "")
        {
            return $"{wxid}-{chatroom}-{rid}";
        }
        /// <summary>
        /// 计算某个wxid的权限
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="chatroom">群id，若为空，则检查所有群的最大权限</param>
        /// <returns></returns>
        private UserRole CalcRole(string wxid, string chatroom = "", string rid = "")
        {
            var wechatCFG = _context.WechatM.WechatCFG();
            if (wechatCFG is not null && wechatCFG.Admins.Contains(wxid)) return UserRole.ADMINISTRATOR;

            var curRole = UserRole.UNDEFINED;
            var user = FindUser(wxid);
            if (user is null) return curRole;

            if (string.IsNullOrEmpty(rid) && string.IsNullOrEmpty(chatroom))
            {
                // ROOMID空/RID空
                if (user.HoldsRIDs is not null && user.HoldsRIDs.Count > 0) return UserRole.GROUP_HOLDER;
                foreach (var _group in _groups.Values)
                {
                    if (_group.Members.ContainsKey(wxid))
                    {
                        if (_group.Members[wxid].Role >= UserRole.GROUP_MANAGER) return UserRole.GROUP_MANAGER;
                        if (_group.Members[wxid].UIDs is not null && _group.Members[wxid].UIDs.Count > 0) curRole = UserRole.NORMAL;
                    }
                }

                if (curRole >= UserRole.NORMAL) return UserRole.NORMAL;

            }
            else if (!string.IsNullOrEmpty(rid))
            {
                if (user.HoldsRIDs is not null && user.HoldsRIDs.Contains(rid)) return UserRole.GROUP_HOLDER;
                var ridGroup = FindGroupByRID(rid);
                if (ridGroup is not null && ridGroup.Members.ContainsKey(wxid))
                {
                    if (ridGroup.Members[wxid].Role >= UserRole.GROUP_MANAGER) return UserRole.GROUP_MANAGER;
                    if (ridGroup.Members[wxid].UIDs is not null && ridGroup.Members[wxid].UIDs.Count > 0) return UserRole.NORMAL;
                }
            }
            else
            {
                var curGroup = FindGroup(chatroom);
                if (curGroup is not null && curGroup.Members.ContainsKey(wxid))
                {
                    if (!string.IsNullOrEmpty(curGroup.RID) && user.HoldsRIDs.Contains(curGroup.RID)) return UserRole.GROUP_HOLDER;
                    if (curGroup.Members[wxid].Role >= UserRole.GROUP_MANAGER) return UserRole.GROUP_MANAGER;
                    if (curGroup.Members[wxid].UIDs is not null && curGroup.Members[wxid].UIDs.Count > 0) return UserRole.NORMAL;
                    if (IsMaydayGroup(chatroom)) return UserRole.PLAYER;
                    if (IsMaydayGroup(chatroom)) return UserRole.MAYDAY;
                }
            }

            return curRole;
        }
        public void ClearGroupMemberRoleCache(string chatroom)
        {
            var group = FindGroup(chatroom);
            if (group is null) return;

            foreach (var member in group.Members)
            {
                SetRoleCache(UserRole.NONE, chatroom, member.Key);
            }
        }

        public void ClearUserRoleCache(string wxid)
        {
            if (string.IsNullOrEmpty(wxid)) return;
            var keys = _roleCache.Keys.Where(x => x.Contains(wxid)).ToList();
            foreach (var key in keys) _roleCache.Remove(key, out _);
        }
        #endregion

        #endregion
    }
}
