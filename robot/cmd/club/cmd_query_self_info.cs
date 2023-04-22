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
    internal class cmd_query_self_info
    {
        public const string Instrus = "查看我的信息";
        public const string Tag = "cmd_query_self_info";
        public const include.ChatScene EnableScene = include.ChatScene.Group;
        public const include.UserRole MinRole = include.UserRole.NORMAL;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                // 找到群
                var group = context.ContactsM.FindGroup(msg.Self, msg.Sender);
                if (group is null)
                {
                    context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请尝试联系超管使用命令\"刷新群信息\"。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                var member = group.Members.ContainsKey(msg.WXID) ? group.Members[msg.WXID] : null;
                if (member is null)
                {
                    context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群内你的资料，请尝试联系会长使用命令\"刷新群信息\"。",
                                             new List<string> { msg.WXID },
                                             msg.Self,
                                             msg.Sender);
                }

                // 找到俱乐部
                var rid = group.RID;
                var club = context.ClubsM.FindClub(msg.Self, rid);
                if (club is null)
                {
                    context.WechatM.SendAtText($"⚠️当前微信群绑定的俱乐部 [{rid}] 不存在。",
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


                var ret = new List<string>();
                ret.Add("为你查到以下信息");
                var role = context.ContactsM.QueryRole(msg.Self, msg.WXID, msg.Sender);
                ret.Add($"在本群权限: {include.wechat.UserRoleDesc(role)}");

                if (club is not null)
                {
                    ret.Add($"本群绑定俱乐部: {club.Name} [{club.RID}]");
                    var uids = member?.UIDs ?? new List<string>();
                    if (uids.Count > 0)
                    {
                        ret.Add($"绑定的游戏角色: \n{string.Join("\n", uids.Select((a) => $"{context.ClubsM.QueryMemberName(msg.Self, a)} [{a}]"))}");
                    }
                    else
                    {
                        if (role <= UserRole.GROUP_HOLDER)
                        {
                            ret.Add($"绑定的游戏角色: (未绑定)");
                        }
                    }
                }

                var result = string.Join("\n", ret);
                if (result is not null) context.WechatM.SendAtText(result, new List<string> { msg.WXID }, msg.Self, msg.Sender);
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
