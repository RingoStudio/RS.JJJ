using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.system
{

    internal class cmd_switch : ICMD
    {
        public Context _context { get; set; }
        public cmd_switch(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "/switch" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_switch";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        async public Task Do(Message msg)
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
                        _context.ConfigsM.SwitchCommunicateClose = !ConvertBool(flag);
                        break;
                    case "登录功能":
                    case "login":
                        desc = "禁用登录功能";
                        _context.ConfigsM.SwitchLoginClose = !ConvertBool(flag);
                        break;  
                    case "图鉴": // TODO
                    case "handbook":
                        desc = "禁用图鉴功能";
                        _context.ConfigsM.SwitchHandbookClose = !ConvertBool(flag);
                        break;
                    case "对话": // TODO
                    case "conversation":
                        desc = "禁用对话功能";
                        _context.ConfigsM.SwitchConversationClose = !ConvertBool(flag);
                        break;
                    case "抽签": // TODO
                    case "qian":
                        desc = "禁用抽签功能";
                        _context.ConfigsM.SwitchQianClose = !ConvertBool(flag);
                        break;
                    case "密令": // TODO
                    case "cdkey":
                        desc = "禁用密令功能";
                        _context.ConfigsM.SwitchCDKeyClose = !ConvertBool(flag);
                        break;
                    default:
                        return;
                }

                _context.WechatM.SendAtText($"已将 [{desc}] 设置为 [{flag}]",
                                               new List<string> { msg.WXID },
                                               msg.Self,
                                               msg.Sender);
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
                _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
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
