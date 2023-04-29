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

    internal class cmd_set_questionnaire_answer : ICMD
    {
        public Context _context { get; set; }
        public cmd_set_questionnaire_answer(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "回答", "答卷", "回复问卷" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_set_questionnaire_answer";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.NORMAL;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        async public Task Do(Message msg)
        {
            try
            {
                var rid = "";
                var arr = msg.ExplodeContent;
                if (arr.Length < 3) return;
                var question = arr[1];
                var answer = arr[2];

                // 未指定rid，则为本群rid
                var group = _context.ContactsM.FindGroup(msg.Self, msg.Sender);
                if (group is null)
                {
                    _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }
                rid = group.RID;

                if (string.IsNullOrEmpty(rid)) return;

                // 检查本俱乐部权限
                if (!_context.ContactsM.CheckGroupRole(msg.Self, rid, msg.WXID, msg.Scene == ChatScene.Group ? msg.Sender : ""))
                {
                    _context.WechatM.SendAtText($"不可以在其他俱乐部群里回复哦。",
                                             new List<string> { msg.WXID },
                                             msg.Self,
                                             msg.Sender);
                    return;
                }

                // 找到俱乐部
                var club = _context.ClubsM.FindClub(msg.Self, rid);
                if (club is null)
                {
                    _context.WechatM.SendAtText($"⚠️俱乐部[{rid}]不存在。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                var member = _context.ContactsM.FindGroupMember(msg.Self, msg.Sender, msg.WXID);
                var uid = "";
                if (member is not null && member.UIDs is not null && member.UIDs.Count > 0) uid = member.UIDs.First();

                var result = _context.QuestionnaireM.SetAnswer(question, answer, msg.Self, club, group, msg.WXID, uid);
                if (result.result)
                {
                    _context.WechatM.SendAtText(_context.QuestionnaireM.QueryResponse(question),
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                }
                else
                {
                    var desc = "很抱歉，";
                    if (!string.IsNullOrEmpty(result.desc)) desc += result.desc;
                    else desc += "发生了未知错误";
                    _context.WechatM.SendAtText(desc,
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
