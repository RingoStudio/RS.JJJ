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

    internal class cmd_del_questionnaire_question
    {
        public Context _context { get; set; }
        public cmd_del_questionnaire_question(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "删除问卷", "移除问卷" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_del_questionnaire_question";
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
                if (string.IsNullOrEmpty(question)) return;
                var existCount = _context.QuestionnaireM.QueryAnswerCount(question);
                if (existCount >= 0)
                {
                    _context.WechatM.SendAtText($"请确认是否删除问题[{question}]？\n" +
                                                $"该问题目前已有{existCount}个回复\n" +
                                                $"请在20秒内回复\"确定\"或\"取消\"",
                                                new List<string> { msg.Sender }, msg.RoomID);
                    _context.CommunicateM.RegistWaitMessageRequest(msg.RoomID, msg.Sender,
                                                                   onReceivedCallback: new Action<Message>((_msg) =>
                                                                   {
                                                                       if (_msg.Content == "确定")
                                                                       {
                                                                           _context.QuestionnaireM.DelQuestion(question);
                                                                           _context.WechatM.SendAtText($"已删除调查问卷问题 [{question}]", new List<string> { msg.Sender }, msg.RoomID);
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
                    _context.WechatM.SendAtText($"问题 [{question}] 不存在。", new List<string> { msg.Sender }, msg.RoomID);
                }


            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
