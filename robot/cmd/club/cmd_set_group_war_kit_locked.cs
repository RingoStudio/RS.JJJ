using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.club
{
    [attribute.CmdClass]
    internal class cmd_set_group_war_kit_locked
    {
        public const string Instrus = "设置锁定套装,锁定套装";
        public const string Tag = "cmd_set_group_war_kit_locked";
        public const include.ChatScene EnableScene = include.ChatScene.Group;
        public const include.UserRole MinRole = include.UserRole.GROUP_MANAGER;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
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

                var group = context.ContactsM.FindGroup(msg.Self, msg.Sender);
                if (group is null)
                {
                    context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
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
                                    context.WechatM.SendAtText($"⚠️没有找到昵称为[{arr[1]}]的群成员。",
                                                               new List<string> { msg.WXID },
                                                               msg.Self,
                                                               msg.Sender);
                                    return;
                                }
                                // 找到多个成员
                                else if (wxids.Count > 1)
                                {
                                    context.WechatM.SendAtText($"⚠️找到多个可能的群成员，昵称和id如下:\n" +
                                                               $"{string.Join("\n", wxids.Select((a) => $"[{context.ContactsM.QueryGroupMemberNick(a, msg.Self, msg.Sender)}]{a}"))}\n",
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
                    var groupMember = context.ContactsM.FindGroupMember(msg.Self, group.WXID, wxid);
                    if (groupMember is null) return;
                    if (groupMember.UIDs is null || groupMember.UIDs.Count == 0)
                    {
                        context.WechatM.SendAtText($"⚠️成员 @{groupMember.NickName} 没有绑定任何游戏角色，设置失败！",
                                                            new List<string> { msg.WXID },
                                                            msg.Self,
                                                            msg.Sender);
                        return;
                    }
                    else if (groupMember.UIDs.Count > 1)
                    {
                        var gameInfos = groupMember.UIDs.Select(x => $"{x}[{context.ClubsM.QueryMemberName(msg.Self, x)}]").ToList();
                        context.WechatM.SendAtText($"⚠️成员 @{groupMember.NickName} 绑定了 {gameInfos.Count} 个游戏角色：\n" +
                                                   string.Join("\n", gameInfos) +
                                                   $"\n你需要使用UID来操作此命令，例如：\n" +
                                                   $"\"锁定套装 {groupMember.UIDs.First()} 苦力\"",
                                                         new List<string> { msg.WXID },
                                                         msg.Self,
                                                         msg.Sender);
                    }
                    else uid = groupMember.UIDs.First();
                }


                // 检查订购
                var purchase = context.PurchaseM.CheckPurchase(rid, msg);
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

                // 检查本俱乐部权限
                if (!context.ContactsM.CheckGroupRole(msg.Self, rid, msg.WXID, msg.Scene == ChatScene.Group ? msg.Sender : ""))
                {
                    context.WechatM.SendAtText($"不可以设置其他俱乐部的信息。",
                                             new List<string> { msg.WXID },
                                             msg.Self,
                                             msg.Sender);
                    return;
                }

                // 找到俱乐部
                var club = context.ClubsM.FindClub(msg.Self, rid);
                if (club is null)
                {
                    context.WechatM.SendAtText($"⚠️要查询的俱乐部[{rid}]不存在。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }



                var result = context.ClubsM.SetClubMemberLockedKit(msg.Self, rid, uid, kit);
                context.WechatM.SendAtText(result.desc ?? (result.result ? "⚠️操作成功。" : "⚠️操作失败。"),
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
