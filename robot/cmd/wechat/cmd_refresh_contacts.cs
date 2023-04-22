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
    internal class cmd_refresh_contacts
    {
        public const string Instrus = "刷新群信息,刷新联系人";
        public const string Tag = "cmd_refresh_contacts";
        public const include.ChatScene EnableScene = include.ChatScene.All;
        public const include.UserRole MinRole = include.UserRole.GROUP_HOLDER;
        public const RS.Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {

                var arr = msg.ExplodeContent;
                var isAll = false;

                if (msg.Scene == include.ChatScene.Private) isAll = true;
                else if (arr.Length > 1)
                {
                    switch (arr[1].ToLower())
                    {
                        case "all":
                        case "全部":
                        case "所有":
                            isAll = true;
                            break;
                        default:
                            isAll = false;
                            break;
                    }
                }


                var role = context.ContactsM.QueryRole(msg.Self, msg.WXID, msg.Sender);
                if (role < include.UserRole.ADMINISTRATOR)
                {
                    if (msg.Scene != include.ChatScene.Group)
                    {
                        //context.WechatM.SendAtText($"你只能在自己的群里使用这个命令。",
                        //                            new List<string> { msg.WXID },
                        //                            msg.Self,
                        //                            msg.Sender);
                        return;
                    }
                    isAll = false;
                }



                if (!isAll)
                {
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
                }



                var result = context.WechatM.RefreshGroupMemberNames(msg.Self, isAll ? "" : msg.Sender);
                var desc = isAll ? "通讯录" : (context.ContactsM.FindGroup(msg.Self, msg.Sender)?.Name ?? "" + "群");

                if (result) context.WechatM.SendAtText($"刷新{desc}信息成功。",
                                                       new List<string> { msg.WXID },
                                                       msg.Self,
                                                       msg.Sender);
                else context.WechatM.SendAtText("因未知原因，操作失败了。",
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
