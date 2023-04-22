using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.club
{
    [attribute.CmdClass]
    internal class cmd_auto_clear_unexist_bindings
    {
        public const string Instrus = "清理成员绑定,清理失效绑定,清理失效成员绑定";
        public const string Tag = "cmd_auto_clear_unexist_bindings";
        public const include.ChatScene EnableScene = include.ChatScene.Group;
        public const include.UserRole MinRole = include.UserRole.GROUP_HOLDER;
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
                    context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
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

                if (club.Members.Count < 20)
                {
                    context.WechatM.SendAtText($"⚠️当前微信群绑定的俱乐部 [{club.Name} {rid}] 成员数量异常。\n" +
                                               $"请重新登录更新成员数据后再试。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                var ret = new List<string>();

                foreach (var member in group.Members)
                {
                    if (member.Value.UIDs is not null && member.Value.UIDs.Count > 0)
                    {
                        member.Value.UIDs = member.Value.UIDs.Where((uid) =>
                        {
                            var flag = club.Members.Contains(uid);
                            if (!flag) ret.Add($"@{member.Value.NickName} -> {context.ClubsM.QueryMemberName(msg.Self, uid) ?? ""}[{uid}]");
                            return flag;
                        }).ToList();

                        if (member.Value.UIDs.Count <= 0 && member.Value.Role < include.UserRole.GROUP_MANAGER)
                        {
                            member.Value.Role = include.UserRole.NONE;
                            context.ContactsM.SetRoleCache(include.UserRole.NONE, member.Key, msg.Self, msg.Sender);
                        }
                    }
                }

                var result = "";
                if (ret.Count > 0) result = $"成功解除 {ret.Count} 条失效成员绑定: \n" +
                                            string.Join("\n", ret);
                else result = "没有找到任何失效成员绑定。";

                context.WechatM.SendAtText(result,
                                           new List<string> { msg.WXID },
                                           msg.Self,
                                           msg.Sender);

            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
                context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
            }
        }
    }
}
