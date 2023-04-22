using RS.Snail.JJJ.boot;
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
    internal class cmd_del_member
    {
        public const string Instrus = "取消成员,移除成员,解绑成员";
        public const string Tag = "cmd_del_member";
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

                // uid
                var uid = arr.Last();
                if (!StringHelper.IsRID(uid)) return;

                // 找到群
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

                // 找到俱乐部
                var rid = group.RID;
                var club = context.ClubsM.FindClub(msg.Self, uid);
                if (club is null)
                {
                    context.WechatM.SendAtText($"⚠️要设置的俱乐部 [{uid}] 不存在。",
                                                                          new List<string> { msg.WXID },
                                                                          msg.Self,
                                                                          msg.Sender);
                    return;
                }

                // 俱乐部是否存在uid
                if (club.Members is null || !club.Members.Contains(uid))
                {
                    context.WechatM.SendAtText($"⚠️要设置的俱乐部 [{club?.Name ?? "新俱乐部"}-{rid}] 中尚未存在UID为[{uid}]的成员。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                var gameNick = context.ClubsM.QueryMemberName(msg.Self, uid) ?? "蜗牛";
                gameNick += $"-{uid}";

                // 已经绑定过此uid
                var wxid = "";
                foreach (var member in group.Members)
                {
                    if (member.Value.UIDs is not null && member.Value.UIDs.Contains(uid))
                    {
                        wxid = member.Key;
                        break;
                    }
                }
                if (string.IsNullOrEmpty(wxid))
                {
                    context.WechatM.SendAtText($"本群内没有任何成员与游戏角色 [{gameNick}] 绑定。",
                                               new List<string> { msg.WXID },
                                               msg.Self,
                                               msg.Sender);
                    return;
                }

                var targetNick = context.ContactsM.QueryGroupMemberNick(wxid, msg.Self, msg.Sender);

                var result = context.ContactsM.DelMember(msg.Self, group.WXID, uid);
                if (result) context.WechatM.SendAtText($"@{targetNick} 已被解除与游戏角色 [{gameNick}] 的绑定。\n",
                                                       new List<string> { msg.WXID },
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
