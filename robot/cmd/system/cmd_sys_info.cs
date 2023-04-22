using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.system
{
    [attribute.CmdClass]
    internal class cmd_sys_info
    {
        public const string Instrus = "/系统信息,/sysinfo";
        public const string Tag = "cmd_sys_info";
        public const include.ChatScene EnableScene = include.ChatScene.All;
        public const include.UserRole MinRole = include.UserRole.ADMINISTRATOR;
        public const RS.Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        [SupportedOSPlatform("windows")]
        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {

            Task.Run(() =>
            {
                try
                {
                    var desc = utils.SystemInfoHelper.GetSystemInfo();
                    context.WechatM.SendAtText($"———— 系统信息 ————\n" +
                                                desc +
                                                $"\n{TimeHelper.ChinsesTimeDesc(TimeHelper.ToTimeStamp())}",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                }
                catch (Exception ex)
                {
                    Context.Logger.Write(ex, Tag);
                    context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                }
            });

        }
    }
}
