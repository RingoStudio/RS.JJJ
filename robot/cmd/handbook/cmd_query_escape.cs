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

    internal class cmd_query_escape : ICMD
    {
        public Context _context { get; set; }
        public cmd_query_escape(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "逃犯", "查询逃犯", "特工", "查询特工" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_query_escape";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.PLAYER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        public void Do(Message msg)
        {
            try
            {
                if (_context.ConfigsM.SwitchHandbookClose) return;
                if (!_context.HandbookM.CheckCD(msg.Sender)) return;

                var arr = msg.ExplodeContent;
                if (arr.Length < 2) return;
                var name = arr.Last();

                var desc = _context.HandbookM.QueryVisitorEscape(name);
                if (desc.Length > 300)
                {
                    var fileName = $"OUT\\逃犯查询结果_@{_context.ContactsM.QueryGroupMemberNickForFile(msg.Sender, msg.RoomID)}_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.txt";
                    fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                    System.IO.File.WriteAllText(fileName, desc);
                    _context.WechatM.SendFile(fileName, msg.RoomID);
                }
                else _context.WechatM.SendAtText(desc, new List<string> { msg.Sender }, msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
