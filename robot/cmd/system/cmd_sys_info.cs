using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Enums;
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

    internal class cmd_sys_info : ICMD
    {
        public Context _context { get; set; }
        public cmd_sys_info(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "/系统信息", "/sysinfo" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_sys_info";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        [SupportedOSPlatform("windows")]
        async public Task Do(Message msg)
        {

            await Task.Run(() =>
               {
                   try
                   {
                       var desc = JJJ.utils.SystemInfoHelper.GetSystemInfo();
                       _context.WechatM.SendAtText($"———— 系统信息 ————\n" +
                                                   desc +
                                                   $"\n{TimeHelper.ChinsesTimeDesc(TimeHelper.ToTimeStamp())}",
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
               });

        }
    }
}
