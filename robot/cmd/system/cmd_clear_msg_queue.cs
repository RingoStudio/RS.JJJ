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
    internal class cmd_clear_msg_queue
    {
        public const string Instrus = "/清理消息队列,/清理消息,/clearmsgqueue";
        public const string Tag = "cmd_clear_msg_queue";
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
                    var isAll = false;
                    if (arr.Length > 1 && (arr[1].ToLower() == "all" || arr[1] == "全部" || arr[1] == "所有")) isAll = true;

                    if (isAll)
                    {
                        context.WechatM.ClearMessageQueue();
                        context.WechatM.SendMgrNotice($"{include.emoji.ZHUYI}已清空待发送的消息队列，已在队列内的回复将全部取消");
                    }
                    else
                    {
                        context.WechatM.ClearMessageQueue(msg.Self);
                        context.WechatM.SendMgrNotice($"{include.emoji.ZHUYI}已清空待发送的消息队列，已在队列内的回复将全部取消", msg.Self);
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
            });

        }
    }
}
