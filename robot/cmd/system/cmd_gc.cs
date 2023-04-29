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

    internal class cmd_gc : ICMD
    {
        public Context _context { get; set; }
        public cmd_gc(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "/释放内存", "/gc" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_gc";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        async public Task Do(Message msg)
        {

            await Task.Run(() =>
             {
                 try
                 {
                     var before = RS.Snail.JJJ.utils.SystemInfoHelper.GetMemory();
                     System.GC.Collect();
                     var after = RS.Snail.JJJ.utils.SystemInfoHelper.GetMemory();
                     _context.WechatM.SendAtText($"释放内存成功\n" +
                                                  $"释放前私有内存占用 {before:N2} MB\n" +
                                                  $"释放后私有内存占用 {after:N2} MB",
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
