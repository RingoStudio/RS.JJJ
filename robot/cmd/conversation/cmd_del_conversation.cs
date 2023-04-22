using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.res.communicate;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.conversation
{
    [attribute.CmdClass]
    internal class cmd_del_conversation
    {
        public const string Instrus = "删除对话";
        public const string Tag = "cmd_del_conversation";
        public const ChatScene EnableScene = ChatScene.All;
        public const UserRole MinRole = UserRole.GROUP_MANAGER;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;
        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                // 删除对话 [OPT:RID] [KEY]
                var rid = "";
                var arr = msg.ExplodeContent;
                var key = "";
                if (arr.Length < 3) return;

                for (int i = 1; i <= arr.Length; i++)
                {
                    if (StringHelper.IsRID(arr[i])) rid = arr[i];
                    else if (string.IsNullOrEmpty(key)) key = arr[i];
                }


                // 未指定rid，则为本群rid
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
                    context.WechatM.SendAtText($"不可以设置其他俱乐部的对话。",
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


                if (string.IsNullOrEmpty(key))
                {
                    var tip = new List<string>();
                    context.WechatM.SendAtText($"在删除对话内容时，您输入了空的关键字，删除失败。",
                                                 new List<string> { msg.WXID },
                                                 msg.Self,
                                                 msg.Sender);
                    return;
                }

                var result = context.ConversationM.UpdateGroupConversation(rid, key, "");
                if (result)
                {
                    var desc = $"俱乐部[{context.ClubsM.QueryClubName(msg.Self, rid) ?? rid}]已经删除了以下对话内容\n" +
                              $"关键字：{key}\n";
                    context.WechatM.SendAtText(desc,
                                               new List<string> { msg.WXID },
                                               msg.Self,
                                               msg.Sender);
                }
                else context.WechatM.SendAtText("可能是因为关键字不存在，或其他原因，删除对话失败了。",
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
