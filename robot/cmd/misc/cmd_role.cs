using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.misc
{
    [attribute.CmdClass]
    internal class cmd_role
    {
        public const string Instrus = "roll,摇骰子,摇色字,随机数";
        public const string Tag = "cmd_roll";
        public const include.ChatScene EnableScene = include.ChatScene.All;
        public const include.UserRole MinRole = include.UserRole.PLAYER;
        public const RS.Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;
        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                var arr = msg.ExplodeContent;
                long max = 6, min = 1;
                if (arr.Length >= 2 && StringHelper.IsNumeric(arr[1])) min = Convert.ToInt64(arr[1]);
                if (arr.Length >= 3 && StringHelper.IsNumeric(arr[2])) max = Convert.ToInt64(arr[2]);
                var list = new List<long> { max, min };
                max = list.Max();
                min = list.Min();
                var result = new Random().NextInt64(min, max + 1);
                context.WechatM.SendAtText($"ROLL {min}~{max} 结果:\n  [{result}]",
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
