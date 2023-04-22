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
    internal class cmd_set_questionnaire_answer
    {
        public const string Instrus = "回答,答卷,回复问卷";
        public const string Tag = "cmd_set_questionnaire_answer";
        public const include.ChatScene EnableScene = include.ChatScene.Group;
        public const include.UserRole MinRole = include.UserRole.NORMAL;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                // 查询物种历史 [OPT:RID]
                var rid = "";
                var arr = msg.ExplodeContent;
                if (arr.Length < 3) return;
                var question = arr[1];
                var answer = arr[2];

                // 未指定rid，则为本群rid
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
                    context.WechatM.SendAtText($"不可以在其他俱乐部群里回复哦。",
                                             new List<string> { msg.WXID },
                                             msg.Self,
                                             msg.Sender);
                    return;
                }

                // 找到俱乐部
                var club = context.ClubsM.FindClub(msg.Self, rid);
                if (club is null)
                {
                    context.WechatM.SendAtText($"⚠️俱乐部[{rid}]不存在。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                var member = context.ContactsM.FindGroupMember(msg.Self, msg.Sender, msg.WXID);
                var uid = "";
                if (member is not null && member.UIDs is not null && member.UIDs.Count > 0) uid = member.UIDs.First();

                var result = context.QuestionnaireM.SetAnswer(question, answer, msg.Self, club, group, msg.WXID, uid);
                if (result.result)
                {
                    context.WechatM.SendAtText("您的回复已被记录，谢谢支持！",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                }
                else
                {
                    var desc = "很抱歉，";
                    if (!string.IsNullOrEmpty(result.desc)) desc += result.desc;
                    else desc += "发生了未知错误";
                    context.WechatM.SendAtText(desc,
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
