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

    internal class cmd_query_cdkey : ICMD
    {
        public Context _context { get; set; }
        public cmd_query_cdkey(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "密令", "查询密令" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_query_cdkey";
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
                int index = 1;
                foreach (var item in arr)
                {
                    if (StringHelper.IsInt(item))
                    {
                        index = Convert.ToInt32(item);
                        break;
                    }
                }

                string desc = "";
                var maxPage = _context.HandbookM.QueryCDKeyPages();
                if (maxPage <= 0) desc = $"没有查询到任何密令，请联系超管重新抓取密令。";
                else if (index > maxPage) desc = $"没有查询到任何密令。\n请尝试用更小的页码查询，例如：\"密令 1\"。";
                else
                {
                    desc = _context.HandbookM.QueryCDKey(index);
                    if (string.IsNullOrEmpty(desc)) desc = $"没有查询到任何密令，请联系超管重新抓取密令。";
                    else desc = $"第{index}页密令如下\n{desc}\n-----------\n当前可查阅1~{maxPage}页密令，\n发送[密令 页码]查询更多密令。";
                }

                if (desc.Length > 300)
                {
                    var fileName = $"OUT\\密令查询结果_@{_context.ContactsM.QueryGroupMemberNickForFile(msg.Sender, msg.RoomID)}_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.txt";
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
