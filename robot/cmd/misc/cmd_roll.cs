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
    
    internal class cmd_roll : ICMD
    {
        public Context _context { get; set; }
        public cmd_roll(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "roll","摇骰子","摇色字","随机数" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_roll";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.PLAYER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        public void Do(Message msg)
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
                _context.WechatM.SendAtText($"ROLL {min}~{max} 结果:\n  [{result}]",
                                            new List<string> { msg.Sender }, msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
