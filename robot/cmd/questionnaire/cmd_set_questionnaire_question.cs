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

    internal class cmd_set_questionnaire_question : ICMD
    {
        public Context _context { get; set; }
        public cmd_set_questionnaire_question(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "设置问卷", "增加问卷", "添加问卷" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_set_questionnaire_question";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        private static string _confirmTag = "cmd_set_questionnaire_question_confirm";

        public void Do(Message msg)
        {
            try
            {
                _context.CommunicateM.UnregistWaitMessageRequest(msg.RoomID, msg.Sender, _confirmTag);
                var arr = msg.ExplodeContent;
                if (arr.Length < 2) return;
                var question = arr[1];
                var response = arr.Length > 2 ? arr[2] : "";
                if (string.IsNullOrEmpty(question)) return;
                var existCount = _context.QuestionnaireM.QueryAnswerCount(question);
                if (existCount > 0)
                {
                    _context.WechatM.SendAtText($"问卷问题 [{question}] 已存在 {existCount} 个回答，是否覆盖？\n" +
                                                $"覆盖后将删除所有已有回答\n" +
                                                $"请在20秒内回复\"确定\"或\"取消\"",
                                                new List<string> { msg.Sender }, msg.RoomID);
                    _context.CommunicateM.RegistWaitMessageRequest(msg.RoomID, msg.Sender,
                                                                   onReceivedCallback: new Action<Message>((_msg) =>
                                                                   {
                                                                       if (_msg.Content == "确定")
                                                                       {
                                                                           _context.QuestionnaireM.SetQuestion(question, response);
                                                                           _context.WechatM.SendAtText($"已增加新的调查问卷问题 [{question}]", new List<string> { msg.Sender }, msg.RoomID);
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
                    _context.QuestionnaireM.SetQuestion(question, response);
                    _context.WechatM.SendAtText($"已增加新的调查问卷问题 [{question}] \n" +
                                                $"所有俱乐部群内已绑定的成员可以回答问卷\n" +
                                                $"现在你可以发送广播通知大家来填写了。",
                                                new List<string> { msg.Sender }, msg.RoomID);
                }
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
