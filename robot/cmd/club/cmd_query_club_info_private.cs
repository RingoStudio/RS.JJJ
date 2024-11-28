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

    internal class cmd_query_club_info_private : ICMD
    {
        public Context _context { get; set; }
        public cmd_query_club_info_private(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "查看俱乐部" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_query_club_info_private";
        public ChatScene EnableScene => ChatScene.Private;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        public void Do(Message msg)
        {
            try
            {
                var arr = msg.ExplodeContent;
                if (arr.Length < 2) return;

                var rid = arr.Last();
                if (!StringHelper.IsRID(rid)) return;


                // 找到俱乐部
                var club = _context.ClubsM.FindClub(rid);
                if (club is null)
                {
                    _context.WechatM.SendText($"⚠️要查询的俱乐部[{rid}]不存在。", msg.RoomID);
                    return;
                }

                // 会长及以下只能查看自己俱乐部
                if (_context.ContactsM.QueryRole(msg.Sender, rid: rid) < MinRole)
                {
                    _context.WechatM.SendAtText($"您没有查看该俱乐部相关信息的权限。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;

                var result = _context.ClubsM.QueryClubInfo(rid, true);
                if (result is not null)
                {
                    if (result.Length > 200)
                    {
                        var fileName = $"OUT\\俱乐部信息查询结果_@{_context.ContactsM.QueryGroupMemberNickForFile(msg.Sender, msg.RoomID)}_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.txt";
                        fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                        System.IO.File.WriteAllText(fileName, result);
                        _context.WechatM.SendFile(fileName, msg.Sender);
                    }
                    else _context.WechatM.SendText(result, msg.Sender);
                }
                else _context.WechatM.SendText("⚠️因未知原因，操作失败了。", msg.Sender);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
