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
    
    internal class cmd_del_member : ICMD
    {
        public Context _context { get; set; }
        public cmd_del_member(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "取消成员", "移除成员", "解绑成员" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_del_member";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        public void Do(Message msg)
        {
            try
            {
                var arr = msg.ExplodeContent;
                if (arr.Length < 2) return;

                // uid
                var uid = arr.Last();
                if (!StringHelper.IsRID(uid)) return;

                // 找到群
                var group = _context.ContactsM.FindGroup(msg.RoomID);
                if (group is not null && !string.IsNullOrEmpty(group.RID))
                {
                    var purchase = _context.PurchaseM.CheckPurchase(group.RID, msg);
                    if (!purchase.result)
                    {
                        if (!string.IsNullOrEmpty(purchase.desc))
                        {
                            _context.WechatM.SendAtText(purchase.desc, new List<string> { msg.Sender }, msg.RoomID);
                        }
                        return;
                    }
                }
                if (group is null)
                {
                    _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                // 找到俱乐部
                var rid = group.RID;
                var club = _context.ClubsM.FindClub(rid);
                if (club is null)
                {
                    _context.WechatM.SendAtText($"⚠️要设置的俱乐部 [{rid}] 不存在。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                // 俱乐部是否存在uid
                if (club.Members is null || !club.Members.Contains(uid))
                {
                    _context.WechatM.SendAtText($"⚠️要设置的俱乐部 [{club?.Name ?? "新俱乐部"}-{rid}] 中尚未存在UID为[{uid}]的成员。",
                                                new List<string> { msg.Sender }, msg.RoomID); ;
                    return;
                }

                var gameNick = _context.ClubsM.QueryMemberName(uid) ?? "蜗牛";
                gameNick += $"-{uid}";

                // 已经绑定过此uid
                var wxid = "";
                foreach (var member in group.Members)
                {
                    if (member.Value.UIDs is not null && member.Value.UIDs.Contains(uid))
                    {
                        wxid = member.Key;
                        break;
                    }
                }
                if (string.IsNullOrEmpty(wxid))
                {
                    _context.WechatM.SendAtText($"本群内没有任何成员与游戏角色 [{gameNick}] 绑定。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                var targetNick = _context.ContactsM.QueryGroupMemberNick(wxid, msg.RoomID);

                var result = _context.ContactsM.DelMember(group.WXID, uid);
                if (result) _context.WechatM.SendAtText($"@{targetNick} 已被解除与游戏角色 [{gameNick}] 的绑定。\n", new List<string> { msg.Sender }, msg.RoomID);
                else _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。", new List<string> { msg.Sender }, msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
