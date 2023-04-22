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
    internal class cmd_gc
    {
        public const string Instrus = "/释放内存,/gc";
        public const string Tag = "cmd_gc";
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
                    var before = utils.SystemInfoHelper.GetMemory();
                    System.GC.Collect();
                    var after = utils.SystemInfoHelper.GetMemory();
                    context.WechatM.SendAtText($"释放内存成功\n" +
                                                 $"释放前私有内存占用 {before:N2} MB\n" +
                                                 $"释放后私有内存占用 {after:N2} MB",
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
