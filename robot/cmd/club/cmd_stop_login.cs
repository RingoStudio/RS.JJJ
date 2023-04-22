using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.game.module;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Snail.JJJ.robot.modules;
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
    internal class cmd_stop_login
    {
        public const string Instrus = "停止登录,终止登录";
        public const string Tag = "cmd_stop_login";
        public const include.ChatScene EnableScene = include.ChatScene.All;
        public const include.UserRole MinRole = include.UserRole.GROUP_MANAGER;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                // 停止登录 [OPT:RID/此俱乐部]
                var arr = msg.ExplodeContent;
                var rid = "";
                if (arr.Length > 1) rid = arr[1];

                // 群聊下可设置为
                var role = context.ContactsM.QueryRole(msg.Self, msg.WXID, msg.Scene == ChatScene.Group ? msg.Sender : "");
                if (rid == "此俱乐部" || rid.ToLower() == "cjlb" || (string.IsNullOrEmpty(rid) && role < UserRole.ADMINISTRATOR))
                {
                    if (msg.Scene == ChatScene.Group) return;
                    var group = context.ContactsM.FindGroup(msg.Self, msg.Sender);
                    if (group is null) return;
                    rid = group.RID ?? "";
                }
                if (StringHelper.IsRID(rid))
                {
                    if (!context.ContactsM.CheckGroupRole(msg.Self, rid, msg.WXID, msg.Scene == ChatScene.Group ? msg.Sender : ""))
                    {
                        context.WechatM.SendAtText($"不可以终止其他俱乐部的登录。",
                                                 new List<string> { msg.WXID },
                                                 msg.Self,
                                                 msg.Sender);
                        return;
                    }
                }
                else rid = "";
                if (role < UserRole.ADMINISTRATOR)
                {
                    if (string.IsNullOrEmpty(rid)) return;
                    else
                    {
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
                    }
                }

                var result = context.SnailsM.StopLogin(rid);
                var desc = string.IsNullOrEmpty(rid) ? "[所有俱乐部]" : $"[{context.ClubsM.QueryClubName(msg.Self, rid) ?? rid}]";
                if (result.result) desc = $"终止{desc}登录成功。";
                else desc = $"终止{desc}登录失败，原因：{result.desc}";

                context.WechatM.SendAtText(desc,
                                            new List<string> { msg.WXID },
                                            msg.Self,
                                            msg.Sender);

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
