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
    internal class cmd_query_club_info_private
    {
        public const string Instrus = "查看俱乐部";
        public const string Tag = "cmd_query_club_info_private";
        public const include.ChatScene EnableScene = include.ChatScene.Private;
        public const include.UserRole MinRole = include.UserRole.GROUP_MANAGER;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                var arr = msg.ExplodeContent;
                if (arr.Length < 2) return;

                var rid = arr.Last();
                if (!StringHelper.IsRID(rid)) return;


                // 找到俱乐部
                var club = context.ClubsM.FindClub(msg.Self, rid);
                if (club is null)
                {
                    context.WechatM.SendText($"⚠️要查询的俱乐部[{rid}]不存在。",
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                // 会长及以下只能查看自己俱乐部
                if (!context.ContactsM.CheckGroupRole(msg.Self, rid, msg.WXID, msg.Scene == ChatScene.Group ? msg.Sender : ""))
                {
                    context.WechatM.SendAtText($"不可以查看其他俱乐部的信息。",
                                             new List<string> { msg.WXID },
                                             msg.Self,
                                             msg.Sender);
                    return;
                }

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

                var result = context.ClubsM.QueryClubInfo(msg.Self, rid, true);
                if (result is not null) context.WechatM.SendText(result, msg.Self, msg.Sender);
                else context.WechatM.SendText("⚠️因未知原因，操作失败了。",
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
