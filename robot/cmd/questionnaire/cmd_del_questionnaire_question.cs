using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.questionnaire
{
    [attribute.CmdClass]
    internal class cmd_det_questionnaire_question
    {
        public const string Instrus = "删除问卷,移除问卷";
        public const string Tag = "cmd_set_questionnaire_question";
        public const ChatScene EnableScene = ChatScene.All;
        public const UserRole MinRole = UserRole.ADMINISTRATOR;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;
        private static string _confirmTag = "cmd_set_questionnaire_question_confirm";
        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                context.CommunicateM.UnregistWaitMessageRequest(msg.Self, msg.Sender, msg.WXID, _confirmTag);
                var arr = msg.ExplodeContent;
                if (arr.Length < 2) return;
                var question = arr[1];
                if (string.IsNullOrEmpty(question)) return;
                var existCount = context.QuestionnaireM.QueryAnswerCount(question);
                if (existCount >= 0)
                {
                    context.WechatM.SendAtText($"请确认是否删除问题[{question}]？\n" +
                                               $"请在20秒内回复\"确定\"或\"取消\"",
                                              new List<string> { msg.WXID },
                                              msg.Self,
                                              msg.Sender);
                    context.CommunicateM.RegistWaitMessageRequest(msg.Self, msg.Sender, msg.WXID,
                                                                 onReceivedCallback: new Action<Message>((_msg) =>
                                                                 {
                                                                     if (_msg.Content == "确定")
                                                                     {
                                                                         context.QuestionnaireM.DelQuestion(question);
                                                                         context.WechatM.SendAtText($"已删除调查问卷问题 [{question}]",
                                                                                                     new List<string> { msg.WXID },
                                                                                                     msg.Self,
                                                                                                     msg.Sender);
                                                                     }
                                                                 }),
                                                                 verifier: new Func<Message, bool>((_msg) =>
                                                                 {
                                                                     return _msg.Content == "确定" || _msg.Content == "取消";
                                                                 }),
                                                                 waitSeconds: 20,
                                                                 tag: _confirmTag);
                }
                else
                {
                    context.WechatM.SendAtText($"问题 [{question}] 不存在。",
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
