using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.cmd.utils;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.mayday
{

    internal class cmd_add_mayday_group : ICMD
    {
        public Context _context { get; set; }
        public cmd_add_mayday_group(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "添加五迷群" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_add_mayday_group";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        public void Do(Message msg)
        {
            try
            {
                var result = _context.MaydayM.AddMaydayGroup(msg.RoomID);
                if (!result.result) _context.WechatM.SendAtText($"⚠️{result.desc}", new List<string> { msg.Sender }, msg.RoomID);
                else _context.WechatM.SendAtText($"恭喜，本群已经成功拥有唧唧叽！\n群里的WMLS可以使用唧唧叽生成门票啦！\n更多玩法正在解锁中。", new List<string> { msg.Sender }, msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
