using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Tools.Common.extension;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.club
{
    [attribute.CmdClass]
    internal class cmd_query_not_binded
    {
        public const string Instrus = "查看未绑定成员";
        public const string Tag = "cmd_query_not_binded";
        public const include.ChatScene EnableScene = include.ChatScene.Group;
        public const include.UserRole MinRole = include.UserRole.GROUP_MANAGER;
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

                var existUIDs = new List<string>();

                foreach (var member in group.Members)
                {
                    if (member.Value.UIDs is not null)
                    {
                        foreach (var uid in member.Value.UIDs)
                        {
                            if (!existUIDs.Contains(uid)) existUIDs.Add(uid);
                        }
                    }
                }

                var ret = club.Members.DeepCopy()
                         .Except(existUIDs)
                         .ToList()
                         .Select((uid) => $"[{context.ClubsM.QueryMemberName(msg.Self, uid)}] {uid}")
                         .ToList();

                var result = "";
                if (ret.Count > 20) result = $"共找到 {ret.Count} 个未绑定游戏角色, 请发送\"查询成员总览\"查看详情。";
                else if (ret.Count > 0) result = $"共找到 {ret.Count} 个未绑定游戏角色 ([昵称] UID): \n" + string.Join("\n", ret);
                else result = "大家都绑定好啦~";

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
