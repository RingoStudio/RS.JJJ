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
    internal class cmd_query_group_war_events_boss_image
    {
        public const string Instrus = "生成事件总表,生成事件总图";
        public const string Tag = "cmd_query_group_war_events_boss_image";
        public const include.ChatScene EnableScene = include.ChatScene.All;
        public const include.UserRole MinRole = include.UserRole.GROUP_MANAGER;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;



        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                // 生成布阵图 [OPT:RID]
                var rid = "";
                var arr = msg.ExplodeContent;
                if (arr.Length > 1 && StringHelper.IsRID(arr[1])) rid = arr[1];

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



                var result = context.ClubsM.GetGroupWarEventsSumImage(msg.Self, rid);
                if (string.IsNullOrEmpty(result) || !System.IO.File.Exists(result)) context.WechatM.SendAtText("⚠️生成失败。",
                                                                            new List<string> { msg.WXID },
                                                                            msg.Self,
                                                                            msg.Sender);
                else context.WechatM.SendImage(result, msg.Self, msg.Sender);


            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
            }
        }
    }
}
