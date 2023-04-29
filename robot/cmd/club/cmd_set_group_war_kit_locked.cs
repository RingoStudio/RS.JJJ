using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.cmd.utils;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.club
{

    internal class cmd_set_group_war_kit_locked : ICMD
    {
        public Context _context { get; set; }
        public cmd_set_group_war_kit_locked(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "设置锁定套装", "锁定套装" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_set_group_war_kit_locked";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        async public Task Do(Message msg)
        {
            try
            {
                var arr = msg.ExplodeContent;
                if (arr.Length < 3) return;
                var uid = "";
                var kit = Kit.UNDEFINED;
                for (int i = 1; i < arr.Length; i++)
                {
                    if (StringHelper.IsRID(arr[i])) uid = arr[i];
                    else
                    {
                        var str = arr[i].Replace("套", "").ToLower();
                        if (str == "勘探" || str == "kt") kit = Kit.KANTAN;
                        else if (str == "苦力" || str == "kl") kit = Kit.KULI;
                        else if (str == "敢死" || str == "gs") kit = Kit.GANSI;
                    }
                }

                // 锁定套装 [@XXX/UID] [套装类型]

                var group = _context.ContactsM.FindGroup(msg.Self, msg.Sender);
                if (group is null)
                {
                    _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }
                var rid = group.RID;
                if (string.IsNullOrEmpty(rid)) return;


                if (!StringHelper.IsRID(uid))
                {
                    // 解析wxid
                    var wxid = "";
                    var ats = msg.AtWxids;
                    if (ats is not null && ats.Length > 0) wxid = ats[0];
                    if (string.IsNullOrEmpty(wxid))
                    {
                        if (arr.Length > 1)
                        {
                            // 判断是否是wxid
                            if (_context.ContactsM.IsGroupMemberWXID(arr[1], msg.Self, msg.Sender))
                            {
                                wxid = arr[1];
                            }
                            else
                            {
                                var wxids = _context.ContactsM.QueryGroupMemberWXID(arr[1], msg.Self, msg.Sender);

                                // 没有找到成员
                                if (wxids is null)
                                {
                                    _context.WechatM.SendAtText($"⚠️没有找到昵称为[{arr[1]}]的群成员。",
                                                               new List<string> { msg.WXID },
                                                               msg.Self,
                                                               msg.Sender);
                                    return;
                                }
                                // 找到多个成员
                                else if (wxids.Count > 1)
                                {
                                    _context.WechatM.SendAtText($"⚠️找到多个可能的群成员，昵称和id如下:\n" +
                                                               $"{string.Join("\n", wxids.Select((a) => $"[{_context.ContactsM.QueryGroupMemberNick(a, msg.Self, msg.Sender)}]{a}"))}\n",
                                                               new List<string> { msg.WXID },
                                                               msg.Self,
                                                               msg.Sender);
                                    return;
                                }

                                wxid = wxids.First();
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(wxid)) return;
                    var groupMember = _context.ContactsM.FindGroupMember(msg.Self, group.WXID, wxid);
                    if (groupMember is null) return;
                    if (groupMember.UIDs is null || groupMember.UIDs.Count == 0)
                    {
                        _context.WechatM.SendAtText($"⚠️成员 @{groupMember.NickName} 没有绑定任何游戏角色，设置失败！",
                                                            new List<string> { msg.WXID },
                                                            msg.Self,
                                                            msg.Sender);
                        return;
                    }
                    else if (groupMember.UIDs.Count > 1)
                    {
                        var gameInfos = groupMember.UIDs.Select(x => $"{x}[{_context.ClubsM.QueryMemberName(msg.Self, x)}]").ToList();
                        _context.WechatM.SendAtText($"⚠️成员 @{groupMember.NickName} 绑定了 {gameInfos.Count} 个游戏角色：\n" +
                                                   string.Join("\n", gameInfos) +
                                                   $"\n你需要使用UID来操作此命令，例如：\n" +
                                                   $"\"锁定套装 {groupMember.UIDs.First()} 苦力\"",
                                                         new List<string> { msg.WXID },
                                                         msg.Self,
                                                         msg.Sender);
                    }
                    else uid = groupMember.UIDs.First();
                }

                // 检查本俱乐部权限
                if (!_context.ContactsM.CheckGroupRole(msg.Self, rid, msg.WXID, msg.Scene == ChatScene.Group ? msg.Sender : ""))
                {
                    _context.WechatM.SendAtText($"不可以设置其他俱乐部的信息。",
                                             new List<string> { msg.WXID },
                                             msg.Self,
                                             msg.Sender);
                    return;
                }

                // 找到俱乐部
                var club = _context.ClubsM.FindClub(msg.Self, rid);
                if (club is null)
                {
                    _context.WechatM.SendAtText($"⚠️要查询的俱乐部[{rid}]不存在。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;

                var result = await Task.Run(() => _context.ClubsM.SetClubMemberLockedKit(msg.Self, rid, uid, kit));
                _context.WechatM.SendAtText(result.desc ?? (result.result ? "⚠️操作成功。" : "⚠️操作失败。"),
                                            new List<string> { msg.WXID },
                                            msg.Self,
                                            msg.Sender);
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
            }
        }
    }
}
