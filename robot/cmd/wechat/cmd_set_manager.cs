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

    internal class cmd_set_manager : ICMD
    {
        public Context _context { get; set; }
        public cmd_set_manager(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "设置经理", "绑定经理" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_set_manager";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.GROUP_HOLDER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        public void Do(Message msg)
        {
            try
            {
                // 设置经理 [@xxx]
                var arr = msg.ExplodeContent;
                if (arr.Length < 2) return;

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
                            _context.WechatM.SendAtText($"⚠️在设置经理时没有找到昵称为[{arr[1]}]的群成员。", new List<string> { msg.Sender }, msg.RoomID);
                            return;
                        }
                        // 找到多个成员
                        else if (wxids.Count > 1)
                        {
                            _context.WechatM.SendAtText($"⚠️在设置经理时找到多个可能的群成员，昵称和id如下:\n" +
                                                        $"{string.Join("\n", wxids.Select((a) => $"[{_context.ContactsM.QueryGroupMemberNick(a, msg.RoomID)}]{a}"))}\n" +
                                                        $"⚠️请使用以上正确的id重新设置，例如\"设置经理 {wxids[0]}\"",
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

                if (group.Members[wxid].Role == include.UserRole.GROUP_MANAGER)
                {
                    var targetNick = _context.ContactsM.QueryGroupMemberNick(wxid, msg.RoomID);
                    _context.WechatM.SendAtText($"{targetNick}已经是本群的经理了。", new List<string> { msg.Sender }, msg.RoomID);
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

                //// 是不是当前会长
                //var curHolder = context.ContactsM.QueryClubHolder(msg.Self, rid);
                //if (string.IsNullOrEmpty(curHolder) || curHolder != msg.WXID)
                //{
                //    context.WechatM.SendAtText($"⚠️你目前不是俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]的会长。\n" +
                //                               $"⚠️此命令必须由当前俱乐部会长本人操作。",
                //                                new List<string> { msg.WXID },
                //                                msg.Self,
                //                                msg.Sender);
                //    return;
                //}

                // 

                var result = _context.ContactsM.SetManager(group.WXID, wxid);
                if (result) _context.WechatM.SendAtText($"您已经被指派本群（{club?.Name ?? "新俱乐部"}-{rid}）经理权限。\n", new List<string> { wxid }, msg.RoomID);
                else _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。", new List<string> { msg.Sender }, msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
