﻿using RS.Snail.JJJ.boot;
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
    internal class cmd_remind_group_war_drill
    {
        public const string Instrus = "提醒钻头";
        public const string Tag = "cmd_remind_group_war_drill";
        public const include.ChatScene EnableScene = include.ChatScene.Group;
        public const include.UserRole MinRole = include.UserRole.GROUP_MANAGER;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                // 提醒钻头 [OPT:MIN] [OPT:INSTRU]
                var arr = msg.ExplodeContent;
                var min = 0;
                var notice = "";
                if (arr.Length > 1)
                {
                    for (int i = 1; i < arr.Length; i++)
                    {
                        if (StringHelper.IsInt(arr[i])) min = Convert.ToInt32(arr[i]);
                        else notice = arr[i];
                    }
                }

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

                var result = context.ClubsM.RemindGroupWarDrill(msg.Self, rid, msg.Sender, msg.WXID, min, notice);
                if (!result.result)
                {
                    if (string.IsNullOrEmpty(result.desc)) context.WechatM.SendAtText("⚠️未查询到任何信息。",
                                                                                    new List<string> { msg.WXID },
                                                                                    msg.Self,
                                                                                    msg.Sender);
                    else context.WechatM.SendAtText($"⚠️{result.result}",
                                                    new List<string> { msg.WXID },
                                                    msg.Self,
                                                    msg.Sender);
                }
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
            }
        }
    }
}