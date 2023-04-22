using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
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
    [attribute.CmdClass]
    internal class cmd_add_club_login_account
    {
        public const string Instrus = "增加俱乐部登录账号,更新俱乐部登录账号";
        public const string Tag = "cmd_add_club_login_account";
        public const include.ChatScene EnableScene = include.ChatScene.All;
        public const include.UserRole MinRole = include.UserRole.GROUP_MANAGER;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                // 增加俱乐部登录账号 [RID] [ACCOUNT] [PASSWORD]
                var arr = msg.ExplodeContent;
                if (arr.Length < 4) return;

                var rid = arr[1];
                if (!StringHelper.IsRID(rid)) return;
                var account = arr[2];
                var password = arr[3];

                if (!context.ContactsM.CheckGroupRole(msg.Self, rid, msg.WXID, msg.Scene == ChatScene.Group ? msg.Sender : ""))
                {
                    context.WechatM.SendAtText($"不可以设置其他俱乐部的登陆账号密码。",
                                             new List<string> { msg.WXID },
                                             msg.Self,
                                             msg.Sender);
                    return;
                }
              

                var club = context.ClubsM.FindClub(msg.Self, rid);
                if (club is null)
                {
                    context.WechatM.SendAtText("没有找到这个俱乐部。",
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

                var result = context.ClubsM.AddClubLoginAccount(msg.Self, rid, account, password);
                if (!result)
                {
                    context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                }
                else
                {
                    context.WechatM.SendAtText($"⚠️已成功更新俱乐部 [{club.Name} {rid}]的账号密码。",
                                               new List<string> { msg.WXID },
                                               msg.Self,
                                               msg.Sender);
                }
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
                context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
            }
        }
    }
}
