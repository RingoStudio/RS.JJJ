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
    internal class cmd_del_conversation_global
    {
        public const string Instrus = "删除全局对话";
        public const string Tag = "cmd_del_conversation_global";
        public const ChatScene EnableScene = ChatScene.All;
        public const UserRole MinRole = UserRole.GROUP_MANAGER;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;
        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                // 删除全局对话 [KEY]
                var rid = "";
                var arr = msg.ExplodeContent;
                var key = "";
                if (arr.Length < 2) return;

                key = arr[1];

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
                    var desc = $"已经删除了以下全局对话内容\n" +
                              $"关键字：{key}\n";
                    context.WechatM.SendAtText(desc,
                                               new List<string> { msg.WXID },
                                               msg.Self,
                                               msg.Sender);
                }
                else context.WechatM.SendAtText("可能是因为关键字不存在，或其他原因，删除全局对话失败了。",
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
