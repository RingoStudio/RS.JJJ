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

    internal class cmd_del_club_login_account : ICMD
    {
        public Context _context { get; set; }
        public cmd_del_club_login_account(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "移除俱乐部登录账号", "删除俱乐部登录账号" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_del_club_login_account";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        public void Do(Message msg)
        {
            try
            {
                // 移除俱乐部登录账号 [RID] [OPT:ACCOUNT]
                var arr = msg.ExplodeContent;
                if (arr.Length < 2) return;

                var rid = arr[1];
                if (!StringHelper.IsRID(rid)) return;
                var account = arr.Length > 2 ? arr[2] : "";

                if (_context.ContactsM.QueryRole(msg.Sender, rid: rid) < MinRole)
                {
                    _context.WechatM.SendAtText($"您没有删除该俱乐部的登录账号密码的权限。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                var club = _context.ClubsM.FindClub(rid);
                if (club is null)
                {
                    _context.WechatM.SendAtText($"未找到俱乐部[{rid}]。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;

                var result = _context.ClubsM.DelClubLoginAccount(rid, account);
                if (!result.result)
                {
                    _context.WechatM.SendAtText($"⚠️{result.desc}，操作失败了。", new List<string> { msg.Sender }, msg.RoomID);
                }
                else
                {
                    _context.WechatM.SendAtText($"⚠️{result.desc}。", new List<string> { msg.Sender }, msg.RoomID);
                }
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
                _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。", new List<string> { msg.Sender }, msg.RoomID);
            }
        }
    }
}
