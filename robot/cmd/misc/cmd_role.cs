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
    
    internal class cmd_role : ICMD
    {
        public Context _context { get; set; }
        public cmd_role(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "roll","摇骰子","摇色字","随机数" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_role";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        async public Task Do(Message msg)
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
