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
    internal class cmd_query_questionnaire_excel
    {
        public const string Instrus = "查询问卷,导出问卷";
        public const string Tag = "cmd_query_questionnaire_excel";
        public const ChatScene EnableScene = ChatScene.All;
        public const UserRole MinRole = UserRole.ADMINISTRATOR;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;
        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                var arr = msg.ExplodeContent;
                var question = "";
                if (arr.Length > 0)
                {
                    question = arr[1];

                }

                if (!context.QuestionnaireM.IsAnyAnswer(question))
                {
                    var desc = "";
                    if (string.IsNullOrEmpty(question)) desc = "调查问卷还没有任何回复";
                    else desc = $"调查问卷[{(question)}]还没人回复";
                    context.WechatM.SendAtText(desc,
                                     new List<string> { msg.WXID },
                                     msg.Self,
                                     msg.Sender);
                    return;
                }
                var result = context.QuestionnaireM.GetQuestionnaireExcel(question);
                if (string.IsNullOrEmpty(result) || !System.IO.File.Exists(result)) context.WechatM.SendAtText("⚠️未查询到任何信息。",
                                                                            new List<string> { msg.WXID },
                                                                            msg.Self,
                                                                            msg.Sender);
                else context.WechatM.SendFile(result, msg.Self, msg.Sender);
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
            }
        }
    }
}
