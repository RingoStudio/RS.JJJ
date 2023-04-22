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
    internal class cmd_gua_qiu
    {
        public const string Instrus = "求卦";
        public const string Tag = "cmd_gua_qiu";
        public const include.ChatScene EnableScene = include.ChatScene.Group;
        public const include.UserRole MinRole = include.UserRole.NORMAL;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                // 求卦 [int]
                int number = 0;
                var arr = msg.ExplodeContent;
                if (arr.Length < 2) return;
                if (!StringHelper.IsInt(arr[1])) return;
                number = Convert.ToInt32(arr[1]);

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


                // 找到俱乐部
                var club = context.ClubsM.FindClub(msg.Self, rid);
                if (club is null)
                {
                    context.WechatM.SendAtText($"⚠️俱乐部[{rid}]不存在。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }



                var result = context.QianM.QiuGua(msg.WXID, number);
                if (string.IsNullOrEmpty(result)) context.WechatM.SendAtText("⚠️未查询到任何信息。",
                                                                            new List<string> { msg.WXID },
                                                                            msg.Self,
                                                                            msg.Sender);
                else
                {
                    if (result.Length <= 200 || context.IsTest)
                    {
                        context.WechatM.SendAtText(result,
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    }
                    else
                    {
                        var fileName = $"OUT\\解卦_{context.ContactsM.QueryGroupMemberNick(msg.WXID, msg.Self, msg.Sender)}_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.txt";
                        fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                        System.IO.File.WriteAllText(fileName, result);
                        context.WechatM.SendFile(fileName, msg.Self, msg.Sender);
                    }
                }
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
            }
        }
    }
}
