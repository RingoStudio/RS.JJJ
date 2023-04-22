using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.club
{
    [attribute.CmdClass]
    internal class cmd_set_club_group_war_event_share_key
    {
        public const string Instrus = "设置物种事件分享密钥,设置物种战争事件分享密钥";
        public const string Tag = "cmd_set_club_group_war_event_share_key";
        public const include.ChatScene EnableScene = include.ChatScene.All;
        public const include.UserRole MinRole = include.UserRole.GROUP_MANAGER;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                // 设置物种事件分享密钥 [OPT:RID] [OPT:KEY]
                var rid = "";
                var arr = msg.ExplodeContent;
                var key = "";
                if (arr.Length > 1)
                {
                    for (int i = 1; i <= arr.Length; i++)
                    {
                        if (StringHelper.IsRID(arr[i])) rid = arr[i];
                        else key = arr[i];
                    }
                }

                // 未指定rid，则为本群rid
                if (string.IsNullOrEmpty(rid))
                {
                    if (msg.Scene == ChatScene.Private) return;
                    else
                    {
                        var group = context.ContactsM.FindGroup(msg.Self, msg.Sender);
                        if (group is null)
                        {
                            context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
                                                        new List<string> { msg.WXID },
                                                        msg.Self,
                                                        msg.Sender);
                            return;
                        }
                        rid = group.RID;
                    }
                }

                if (string.IsNullOrEmpty(rid)) return;

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
                    context.WechatM.SendAtText($"不可以查看其他俱乐部的信息。",
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



                club.GroupwarEventShareKey = key.Trim();
                var desc = "";
                if (string.IsNullOrEmpty(club.GroupwarEventShareKey))
                {
                    desc = $"已清空俱乐部[{club.Name}]的物种事件分享密钥";
                }
                else
                {
                    desc = $"已将俱乐部[{club.Name}]的物种事件分享密钥设置为[{club.GroupwarEventShareKey}]\n" +
                           $"从下一次登录之后，该俱乐部新增的物种事件列表将被独立保存；\n" +
                           $"同一大区下，设置同样密钥的俱乐部可以共享这部分事件；\n" +
                           $"同一大区下，设置密钥的俱乐部可以查询到未设置密钥的俱乐部共享出的事件。";
                }
                context.WechatM.SendAtText(desc,
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
