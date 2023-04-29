using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.res.communicate;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.conversation
{

    internal class cmd_del_conversation_global : ICMD
    {
        public Context _context { get; set; }
        public cmd_del_conversation_global(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "删除全局对话" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_del_conversation_global";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        async public Task Do(Message msg)
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
                    _context.WechatM.SendAtText($"在删除对话内容时，您输入了空的关键字，删除失败。",
                                                 new List<string> { msg.WXID },
                                                 msg.Self,
                                                 msg.Sender);
                    return;
                }

                var result = _context.ConversationM.UpdateGroupConversation(rid, key, "");
                if (result)
                {
                    var desc = $"已经删除了以下全局对话内容\n" +
                              $"关键字：{key}\n";
                    _context.WechatM.SendAtText(desc,
                                               new List<string> { msg.WXID },
                                               msg.Self,
                                               msg.Sender);
                }
                else _context.WechatM.SendAtText("可能是因为关键字不存在，或其他原因，删除全局对话失败了。",
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
