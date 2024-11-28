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
    internal class cmd_set_member : ICMD
    {
        public Context _context { get; set; }
        public cmd_set_member(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "设置成员", "绑定成员" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_set_member";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        public void Do(Message msg)
        {
            try
            {
                // 设置成员 [@xxx] [UID] 
                var arr = msg.ExplodeContent;
                if (arr.Length < 3) return;

                // rid
                var uid = arr.Last();
                if (!StringHelper.IsRID(uid)) return;

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

                // 解析wxid
                var wxid = "";
                var ats = msg.AtWxids;
                if (ats is not null && ats.Count > 0) wxid = ats[0];
                if (string.IsNullOrEmpty(wxid))
                {
                    // 判断是否是wxid
                    if (_context.ContactsM.IsGroupMemberWXID(arr[1], msg.RoomID))
                    {
                        wxid = arr[1];
                    }
                    else
                    {
                        var wxids = _context.ContactsM.QueryGroupMemberWXID(arr[1], msg.RoomID);

                        // 没有找到成员
                        if (wxids is null || wxids.Count == 0)
                        {
                            _context.WechatM.SendAtText($"⚠️在设置成员时没有找到昵称为[{arr[1]}]的群成员。", new List<string> { msg.Sender }, msg.RoomID);
                            return;
                        }
                        // 找到多个成员
                        else if (wxids.Count > 1)
                        {
                            _context.WechatM.SendAtText($"⚠️在设置成员时找到多个可能的群成员，昵称和id如下:\n" +
                                                       $"{string.Join("\n", wxids.Select((a) => $"[{_context.ContactsM.QueryGroupMemberNick(a, msg.RoomID)}]{a}"))}\n" +
                                                       $"⚠️请使用以上正确的id重新设置，例如\"{arr.First()} {wxids[0]} {uid}\"",
                                                       new List<string> { msg.Sender }, msg.RoomID);
                            return;
                        }

                        wxid = wxids.First();
                    }
                }


                // 找到群成员
                if (!group.Members.ContainsKey(wxid))
                {
                    _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群内该成员的资料，请联系会长使用命令\"刷新群信息\"。",
                                                new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                // 找到俱乐部
                var rid = group.RID;
                var club = _context.ClubsM.FindClub(rid);
                if (club is null)
                {
                    _context.WechatM.SendAtText($"⚠️要设置的俱乐部[{rid}]不存在。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                // 俱乐部是否存在uid
                if (club.Members is null || !club.Members.Contains(uid))
                {
                    _context.WechatM.SendAtText($"⚠️要设置的俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]中尚未存在UID为[{uid}]的成员。\n" +
                                                $"请确认要绑定的游戏角色是否在当前俱乐部中。\n" +
                                                $"新加入唧唧叽的俱乐部请先执行一次\"启动登录 此俱乐部\"，再进行成员绑定。",
                                                new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                // 已经绑定过此uid
                if (group.Members[wxid].UIDs is not null && group.Members[wxid].UIDs.Contains(uid))
                {
                    var targetNick = _context.ContactsM.QueryGroupMemberNick(wxid, msg.RoomID);
                    _context.WechatM.SendAtText($"@{targetNick}已经绑定该游戏角色，无需再次绑定。",
                                                new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                var gameNick = _context.ClubsM.QueryMemberName(uid) ?? "蜗牛";
                gameNick += $"-{uid}";

                // 其他人绑定
                foreach (var member in group.Members)
                {
                    if (member.Value.UIDs is not null && member.Value.UIDs.Contains(uid))
                    {

                        _context.WechatM.SendAtText($"游戏角色[{gameNick}]已经被本群其他成员 @{member.Value.NickName} 绑定。\n" +
                                                    $"⚠️如需解除原有绑定，请发送\"解绑成员 {uid}\"",
                                                    new List<string> { msg.Sender }, msg.RoomID);
                        return;
                    }
                }

                var result = _context.ContactsM.SetMember(group.WXID, wxid, uid);
                if (result) _context.WechatM.SendAtText($"您已成功与俱乐部 [{club?.Name ?? "新俱乐部"}-{rid}] 中的 [{gameNick}] 绑定。\n",
                                                        new List<string> { wxid }, msg.RoomID);
                else _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。", new List<string> { msg.Sender }, msg.RoomID);

            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
