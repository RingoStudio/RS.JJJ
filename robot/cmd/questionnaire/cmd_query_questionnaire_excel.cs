using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.questionnaire
{
    
    internal class cmd_query_questionnaire_excel : ICMD
    {
        public Context _context { get; set; }
        public cmd_query_questionnaire_excel(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "查询问卷", "导出问卷" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_query_questionnaire_excel";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        async public Task Do(Message msg)
        {
            try
            {
                var arr = msg.ExplodeContent;
                var question = "";
                if (arr.Length > 0)
                {
                    question = arr[1];

                }

                await Task.Run(() =>
                {
                    if (!_context.QuestionnaireM.IsAnyAnswer(question))
                    {
                        var desc = "";
                        if (string.IsNullOrEmpty(question)) desc = "调查问卷还没有任何回复";
                        else desc = $"调查问卷[{(question)}]还没人回复";
                        _context.WechatM.SendAtText(desc,
                                         new List<string> { msg.WXID },
                                         msg.Self,
                                         msg.Sender);
                        return;
                    }

                    var result = _context.QuestionnaireM.GetQuestionnaireExcel(question);
                    if (string.IsNullOrEmpty(result) || !System.IO.File.Exists(result)) _context.WechatM.SendAtText("⚠️未查询到任何信息。",
                                                                                                                    new List<string> { msg.WXID },
                                                                                                                    msg.Self,
                                                                                                                    msg.Sender);
                    else _context.WechatM.SendFile(result, msg.Self, msg.Sender);
                });
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
            }
        }
    }
}
