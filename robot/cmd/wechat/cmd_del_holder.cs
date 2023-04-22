using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.wechat
{
    [attribute.CmdClass]
    internal class cmd_del_holder
    {
        public const string Instrus = "取消会长,移除会长,解绑会长";
        public const string Tag = "cmd_del_holder";
        public const include.ChatScene EnableScene = include.ChatScene.Group;
        public const include.UserRole MinRole = include.UserRole.ADMINISTRATOR;
        public const RS.Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                // 取消会长 [CLUB RID]
                var arr = msg.ExplodeContent;
                if (arr.Length < 2) return;

                // 解析rid
                var rid = arr.Last();
                if (!StringHelper.IsRID(rid)) return;

                // 找到俱乐部
                var club = context.ClubsM.FindClub(msg.Self, rid);

                if (club is null)
                {
                    context.WechatM.SendAtText($"⚠️要设置的俱乐部[{rid}]不存在。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                var curHolder = context.ContactsM.QueryClubHolder(msg.Self, rid);
                if (string.IsNullOrEmpty(curHolder))
                {
                    context.WechatM.SendAtText($"⚠️俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]目前尚未指派会长。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                var result = context.ContactsM.DelHolder(msg.Self, rid);

                if (result) context.WechatM.SendAtText($"俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]的会长已被移除。\n" +
                                                       $"⚠️请尽快指派新的会长。",
                                                       new List<string> { msg.WXID },
                                                       msg.Self,
                                                       msg.Sender);
                else context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
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
