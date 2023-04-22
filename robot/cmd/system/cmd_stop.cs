using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.system
{
    [attribute.CmdClass]
    internal class cmd_stop
    {
        public const string Instrus = "/关机/关叽,/stop,/exit,/shutdown";
        public const string Tag = "cmd_reboot";
        public const include.ChatScene EnableScene = include.ChatScene.All;
        public const include.UserRole MinRole = include.UserRole.ADMINISTRATOR;
        public const RS.Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;
        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {

            Task.Run(() =>
            {
                try
                {
                    var arr = msg.ExplodeContent;
                    var interval = 60;
                    if (arr.Length > 1 && StringHelper.IsInt(arr[1])) interval = Convert.ToInt32(arr[1]);
                    interval = Math.Max(30, interval);

                    context.Exit(interval);
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
