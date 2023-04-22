using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.game.module;
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
    internal class cmd_start_login
    {
        public const string Instrus = "启动登录,开始登录";
        public const string Tag = "cmd_start_login";
        public const include.ChatScene EnableScene = include.ChatScene.All;
        public const include.UserRole MinRole = include.UserRole.GROUP_MANAGER;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                // 启动登录 [OPT:RID/此俱乐部] [OPT:演练战力]
                var arr = msg.ExplodeContent;
                var rid = "";
                var armyCombat = false;
                var thisGroup = false;
                if (arr.Length > 1)
                {
                    for (int i = 1; i < arr.Length; i++)
                    {
                        if (arr[i] == "演练战力" || arr[i].ToLower() == "ylzl") armyCombat = true;
                        else if (arr[i] == "此俱乐部" || arr[i].ToLower() == "cjlb") thisGroup = true;
                        else if (StringHelper.IsRID(arr[i])) rid = arr[i];
                    }
                }

                var role = context.ContactsM.QueryRole(msg.Self, msg.WXID, msg.Scene == ChatScene.Group ? msg.Sender : "");
                Group? group = null;
                Club? club = null;
                if (msg.Scene == ChatScene.Private)
                {
                    if (!StringHelper.IsRID(rid)) return;
                    club = context.ClubsM.FindClub(msg.Self, rid);
                    if (club is null) return;
                }
                else
                {
                    if (thisGroup)
                    {
                        group = context.ContactsM.FindGroup(msg.Self, msg.Sender);
                        if (group is null) return;
                        rid = group.RID;
                        club = context.ClubsM.FindClub(msg.Self, rid);
                        if (club is null) return;
                    }
                    else if (StringHelper.IsRID(rid))
                    {
                        club = context.ClubsM.FindClub(msg.Self, rid);
                        if (club is null) return;
                    }
                }

                if (role != UserRole.ADMINISTRATOR)
                {
                    if (club is null) return;
                    if (armyCombat) return;

                    if (!context.ContactsM.CheckGroupRole(msg.Self, club.RID, msg.WXID, msg.Scene == ChatScene.Group ? msg.Self : ""))
                    {
                        context.WechatM.SendAtText($"不可以查看其他俱乐部的信息。",
                                                 new List<string> { msg.WXID },
                                                 msg.Self,
                                                 msg.Sender);
                        return;
                    }

                    var purchase = context.PurchaseM.CheckPurchase(club.RID, msg);
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

                if (club is null && armyCombat) return;

                if (club is null)
                {
                    var result = context.SnailsM.StartLoginAll(msg.Self);
                    var list = new List<string>();
                    foreach (var item in result)
                    {
                        if (!item.Value.result)
                        {
                            list.Add($"[{context.ClubsM.QueryClubName(msg.Self, item.Key) ?? item.Key}]: {item.Value.desc}");
                        }
                    }

                    var desc = "";
                    if (list.Count == 0) desc = $"全部 {result.Count} 个俱乐部进入登录队列";
                    else
                    {
                        desc = $"当前共有 {result.Count} 个俱乐部，其中 {list.Count} 个俱乐部操作失败，原因如下：\n" +
                               string.Join("\n", list);
                    }
                    context.WechatM.SendAtText(desc,
                                             new List<string> { msg.WXID },
                                             msg.Self,
                                             msg.Sender);
                }
                else
                {
                    // var isAdmin = context.WechatM.FindWechatCFG(msg.Self)?.Admins?.Contains(rid) ?? false;
                    var result = context.SnailsM.StartLogin(msg.Self, club.RID, isJustSonArmy: armyCombat);
                    var desc = "";
                    if (result.result) desc = "启动俱乐部登录成功";
                    else desc = $"启动俱乐部登录失败，原因：{result.desc}";
                    context.WechatM.SendAtText(desc,
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
