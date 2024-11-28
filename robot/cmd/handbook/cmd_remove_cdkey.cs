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

namespace RS.Snail.JJJ.robot.cmd.club
{

    internal class cmd_remove_cdkey : ICMD
    {
        public Context _context { get; set; }
        public cmd_remove_cdkey(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "过期密令", "删除密令" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_remove_cdkey";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.GROUP_HOLDER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        public void Do(Message msg)
        {
            try
            {
                var keys = string.Join(" ", msg.ExplodeContent.Skip(1)).Replace("\r", "").Replace(" ", "\n").Split("\n");
                if (keys is null || keys.Length == 0) return;

                var result = _context.HandbookM.AddOldCDKeys(keys.ToList());

                _context.WechatM.SendAtText(result.desc ?? "⚠️因未知原因，操作失败了。", new List<string> { msg.Sender }, msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
