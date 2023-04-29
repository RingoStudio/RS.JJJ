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

namespace RS.Snail.JJJ.robot.cmd.wechat
{
    
    internal class cmd_del_holder : ICMD
    {
        public Context _context { get; set; }
        public cmd_del_holder(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "取消会长", "移除会长", "解绑会长" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_del_holder";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        async public Task Do(Message msg)
        {
            try
            {
                // 取消会长 [CLUB RID]
                var arr = msg.ExplodeContent;
                if (arr.Length < 2) return;

                // 解析rid
                var rid = arr.Last();
                if (!StringHelper.IsRID(rid)) return;

                // 找到俱乐部
                var club = _context.ClubsM.FindClub(msg.Self, rid);

                if (club is null)
                {
                    _context.WechatM.SendAtText($"⚠️要设置的俱乐部[{rid}]不存在。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                var curHolder = _context.ContactsM.QueryClubHolder(msg.Self, rid);
                if (string.IsNullOrEmpty(curHolder))
                {
                    _context.WechatM.SendAtText($"⚠️俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]目前尚未指派会长。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                var result = _context.ContactsM.DelHolder(msg.Self, rid);
                if (result) _context.WechatM.SendAtText($"俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]的会长已被移除。\n" +
                                                       $"⚠️请尽快指派新的会长。",
                                                       new List<string> { msg.WXID },
                                                       msg.Self,
                                                       msg.Sender);
                else _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
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
