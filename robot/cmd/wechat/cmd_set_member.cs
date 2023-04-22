﻿using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.wechat
{
    [attribute.CmdClass]
    internal class cmd_set_member
    {
        public const string Instrus = "设置成员,绑定成员";
        public const string Tag = "cmd_set_member";
        public const include.ChatScene EnableScene = include.ChatScene.Group;
        public const include.UserRole MinRole = include.UserRole.GROUP_MANAGER;
        public const RS.Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                // 设置成员 [@xxx] [UID] 
                var arr = msg.ExplodeContent;
                if (arr.Length < 3) return;

                // rid
                var uid = arr.Last();
                if (!StringHelper.IsRID(uid)) return;

                var group = context.ContactsM.FindGroup(msg.Self, msg.Sender);
                if (group is not null && !string.IsNullOrEmpty(group.RID))
                {
                    var purchase = context.PurchaseM.CheckPurchase(group.RID, msg);
                    if (!purchase.result)
                    {
                        if (!string.IsNullOrEmpty(purchase.desc))
                        {
                            context.WechatM.SendAtText(purchase.desc,
                                                  new List<string> { msg.WXID },
                                                  msg.Self,
                                                  msg.Sender);
                        }
                        return;
                    }
                }
                if (group is null)
                {
                    context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                // 解析wxid
                var wxid = "";
                var ats = msg.AtWxids;
                if (ats is not null && ats.Length > 0) wxid = ats[0];
                if (string.IsNullOrEmpty(wxid))
                {
                    // 判断是否是wxid
                    if (context.ContactsM.IsGroupMemberWXID(arr[1], msg.Self, msg.Sender))
                    {
                        wxid = arr[1];
                    }
                    else
                    {
                        var wxids = context.ContactsM.QueryGroupMemberWXID(arr[1], msg.Self, msg.Sender);

                        // 没有找到成员
                        if (wxids is null)
                        {
                            context.WechatM.SendAtText($"⚠️在设置成员时没有找到昵称为[{arr[1]}]的群成员。",
                                                       new List<string> { msg.WXID },
                                                       msg.Self,
                                                       msg.Sender);
                            return;
                        }
                        // 找到多个成员
                        else if (wxids.Count > 1)
                        {
                            context.WechatM.SendAtText($"⚠️在设置成员时找到多个可能的群成员，昵称和id如下:\n" +
                                                       $"{string.Join("\n", wxids.Select((a) => $"[{context.ContactsM.QueryGroupMemberNick(a, msg.Self, msg.Sender)}]{a}"))}\n" +
                                                       $"⚠️请使用以上正确的id重新设置，例如\"{arr.First()} {wxids[0]} {uid}\"",
                                                       new List<string> { msg.WXID },
                                                       msg.Self,
                                                       msg.Sender);
                            return;
                        }

                        wxid = wxids.First();
                    }
                }


                // 找到群成员
                if (!group.Members.ContainsKey(wxid))
                {
                    context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群内该成员的资料，请联系会长使用命令\"刷新群信息\"。",
                                               new List<string> { msg.WXID },
                                               msg.Self,
                                               msg.Sender);
                    return;
                }

                // 找到俱乐部
                var rid = group.RID;
                var club = context.ClubsM.FindClub(msg.Self, uid);
                if (club is null)
                {
                    context.WechatM.SendAtText($"⚠️要设置的俱乐部[{uid}]不存在。",
                                                                          new List<string> { msg.WXID },
                                                                          msg.Self,
                                                                          msg.Sender);
                    return;
                }

                // 俱乐部是否存在uid
                if (club.Members is null || !club.Members.Contains(uid))
                {
                    context.WechatM.SendAtText($"⚠️要设置的俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]中尚未存在UID为[{uid}]的成员。\n" +
                                               $"请确认要绑定的游戏角色是否在当前俱乐部中。\n" +
                                               $"新加入唧唧叽的俱乐部请先执行一次\"启动登录 此俱乐部\"，再进行成员绑定。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                // 已经绑定过此uid
                if (group.Members[wxid].UIDs is not null && group.Members[wxid].UIDs.Contains(uid))
                {
                    var targetNick = context.ContactsM.QueryGroupMemberNick(wxid, msg.Self, msg.Sender);
                    context.WechatM.SendAtText($"@{targetNick}已经绑定该游戏角色，无需再次绑定。",
                                               new List<string> { msg.WXID },
                                               msg.Self,
                                               msg.Sender);
                    return;
                }

                var gameNick = context.ClubsM.QueryMemberName(msg.Self, uid) ?? "蜗牛";
                gameNick += $"-{uid}";

                // 其他人绑定
                foreach (var member in group.Members)
                {
                    if (member.Value.UIDs is not null && member.Value.UIDs.Contains(uid))
                    {

                        context.WechatM.SendAtText($"游戏角色[{gameNick}]已经被本群其他成员 @{member.Value.NickName} 绑定。\n" +
                                                   $"⚠️如需解除原有绑定，请发送\"解绑成员 {uid}\"",
                                                  new List<string> { msg.WXID },
                                                  msg.Self,
                                                  msg.Sender);
                        return;
                    }
                }

                var result = context.ContactsM.SetMember(msg.Self, group.WXID, wxid, uid);
                if (result) context.WechatM.SendAtText($"您已经与俱乐部 [{club?.Name ?? "新俱乐部"}-{uid}] 中 [{gameNick}] 的绑定。\n",
                                                       new List<string> { wxid },
                                                       msg.Self,
                                                       msg.Sender);
                else context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
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
