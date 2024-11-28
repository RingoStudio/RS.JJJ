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

    internal class cmd_reboot : ICMD
    {
        public Context _context { get; set; }
        public cmd_reboot(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "/重启", "/reboot", "/restart" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_reboot";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        public void Do(Message msg)
        {
            try
            {
                var arr = msg.ExplodeContent;
                var interval = 60;
                if (arr.Length > 1 && StringHelper.IsInt(arr[1])) interval = Convert.ToInt32(arr[1]);
                interval = Math.Max(30, interval);

                _context.Restart(interval);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
                _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。", new List<string> { msg.Sender }, msg.RoomID);
            }
        }
    }
}
