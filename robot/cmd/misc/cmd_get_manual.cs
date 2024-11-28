using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.misc
{

    internal class cmd_get_manual : ICMD
    {
        public Context _context { get; set; }
        public cmd_get_manual(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "唧唧叽说明书", "唧唧叽使用说明", "唧唧叽用户手册", "帮助", "help" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_get_manual";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        public void Do(Message msg)
        {
            try
            {
                _context.WechatM.SendArtical(name: "唧唧叽用户手册",
                                             account: "gh_93e1f831311e", // 用飞碟的最强蜗牛头像
                                             imagefilePath: System.IO.Path.GetFullPath("BOT\\jjj_instruction_cover.png"),
                                             url: "https://docs.qq.com/doc/p/4d2394c38d6fe94ee050b44a5a5d65f89f7c4bc0",
                                             title: "唧唧叽用户手册",
                                             @abstract: "©RingoMango | 冰法集团 荣誉出品",
                                             msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
