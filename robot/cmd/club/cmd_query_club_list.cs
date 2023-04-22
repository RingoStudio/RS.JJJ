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

namespace RS.Snail.JJJ.robot.cmd.club
{
    [attribute.CmdClass]
    internal class cmd_query_club_list
    {
        public const string Instrus = "查询俱乐部名单";
        public const string Tag = "cmd_query_club_list";
        public const include.ChatScene EnableScene = include.ChatScene.All;
        public const include.UserRole MinRole = include.UserRole.ADMINISTRATOR;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                var result = context.ClubsM.GetClubListExcel(msg.Self);
                if (string.IsNullOrEmpty(result) || !System.IO.File.Exists(result))
                {
                    context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
                                            new List<string> { msg.WXID },
                                            msg.Self,
                                            msg.Sender);
                }
                else
                {
                    context.WechatM.SendFile(result, msg.Self, msg.Sender);

                }
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
                context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
            }
        }
    }
}
