using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.game.module;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.cmd.utils;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.club
{

    internal class cmd_start_login : ICMD
    {
        public Context _context { get; set; }
        public cmd_start_login(Context context)
        {
            _context = context;
        }
        public List<string> Commands { get; } = new List<string> { "启动登录", "开始登录" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag { get; } = "cmd_start_login";
        public ChatScene EnableScene { get; } = include.ChatScene.All;
        public UserRole MinRole { get; } = include.UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType { get; } = Tools.Common.Enums.WechatMessageType.Text;

        public void Do(Message msg)
        {
            try
            {
                // 启动登录 [OPT:RID/此俱乐部] [OPT:演练战力]
                var arr = msg.ExplodeContent;
                var rid = "";
                var arenaAttribs = false;
                var thisGroup = false;
                var needRemind = false;
                if (arr.Length > 1)
                {
                    for (int i = 1; i < arr.Length; i++)
                    {
                        if (arr[i] == "演练战力" || arr[i].ToLower() == "ylzl" || arr[i] == "演练九维" || arr[i].ToLower() == "yljw") arenaAttribs = true;
                        else if (arr[i] == "此俱乐部" || arr[i].ToLower() == "cjlb") thisGroup = true;
                        else if (arr[i] == "提醒" || arr[i].ToLower() == "tx") needRemind = true;
                        else if (StringHelper.IsRID(arr[i])) rid = arr[i];
                    }
                }

                Group? group = null;
                Club? club = null;
                if (msg.Scene == ChatScene.Private)
                {
                    if (!StringHelper.IsRID(rid)) return;
                    club = _context.ClubsM.FindClub(rid);
                    if (club is null) return;
                }
                else
                {
                    if (thisGroup)
                    {
                        group = _context.ContactsM.FindGroup(msg.RoomID);
                        if (group is null) return;
                        rid = group.RID;
                        club = _context.ClubsM.FindClub(rid);
                        if (club is null) return;
                    }
                    else if (StringHelper.IsRID(rid))
                    {
                        club = _context.ClubsM.FindClub(rid);
                        if (club is null) return;
                    }
                }

                if (!_context.ContactsM.IsAdmin(msg.Sender))
                {
                    if (club is null) return;
                    if (arenaAttribs) return;

                    if (_context.ContactsM.QueryRole(msg.Sender, rid: club.RID) < MinRole)
                    {
                        _context.WechatM.SendAtText($"您没有查看该俱乐部相关信息的权限。", new List<string> { msg.Sender }, msg.RoomID);
                        return;
                    }

                    if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;
                }

                if (club is null && arenaAttribs) return;

                if (club is null)
                {
                    var result = _context.SnailsM.StartLoginAllManual(needAttribs: arenaAttribs, needRemind: needRemind);
                    var list = new List<string>();
                    foreach (var item in result)
                    {
                        if (!item.Value.result)
                        {
                            list.Add($"[{_context.ClubsM.QueryClubName(item.Key) ?? item.Key}]: {item.Value.desc}");
                        }
                    }

                    var desc = "";
                    if (list.Count == 0) desc = $"全部 {result.Count} 个俱乐部进入登录队列";
                    else
                    {
                        desc = $"当前共有 {result.Count} 个俱乐部，其中 {list.Count} 个俱乐部操作失败，原因如下：\n" +
                               string.Join("\n", list);
                    }
                    _context.WechatM.SendAtText(desc, new List<string> { msg.Sender }, msg.RoomID);
                }
                else
                {
                    var isAdmin = _context.ContactsM.IsAdmin(msg.Sender);
                    var result = _context.SnailsM.StartLogin(club.RID, isAdministrator: isAdmin, isJustAttribs: arenaAttribs, needRemind: needRemind);
                    var desc = "";
                    if (result.result) desc = "启动俱乐部登录成功";
                    else desc = $"启动俱乐部登录失败，原因：{result.desc}";
                    _context.WechatM.SendAtText(desc, new List<string> { msg.Sender }, msg.RoomID);
                }

            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
                _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。", new List<string> { msg.Sender }, msg.RoomID);
            }
        }
    }
}
