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
    internal class cmd_switch
    {
        public const string Instrus = "/switch";
        public const string Tag = "cmd_switch";
        public const include.ChatScene EnableScene = include.ChatScene.All;
        public const include.UserRole MinRole = include.UserRole.ADMINISTRATOR;
        public const RS.Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;
        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                var arr = msg.ExplodeContent;
                if (arr.Length < 3) return;

                var command = arr[1].ToLower();
                var flag = arr[2];
                var desc = "";
                switch (command)
                {
                    case "全局响应":
                    case "communicate":
                        desc = "禁用全局响应";
                        context.ConfigsM.SwitchCommunicateClose = !ConvertBool(flag);
                        break;
                    case "登录功能":
                    case "login":
                        desc = "禁用登录功能";
                        context.ConfigsM.SwitchLoginClose = !ConvertBool(flag);
                        break;
                    case "图鉴":
                    case "handbook":
                        desc = "禁用图鉴功能";
                        context.ConfigsM.SwitchHandbookClose = !ConvertBool(flag);
                        break;
                    case "对话":
                    case "conversation":
                        desc = "禁用对话功能";
                        context.ConfigsM.SwitchConversationClose = !ConvertBool(flag);
                        break;
                    case "抽签":
                    case "qian":
                        desc = "禁用抽签功能";
                        context.ConfigsM.SwitchQianClose = !ConvertBool(flag);
                        break;
                    case "密令":
                    case "cdkey":
                        desc = "禁用密令功能";
                        context.ConfigsM.SwitchCDKeyClose = !ConvertBool(flag);
                        break;
                    default:
                        return;
                }

                context.WechatM.SendAtText($"已将 [{desc}] 设置为 [{flag}]",
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
        }

        private static bool ConvertBool(string msg) => msg.ToLower() switch
        {
            "开" or "开启" or "on" or "true" => true,
            _ => false,
        };
    }
}
