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

    internal class cmd_set_holder : ICMD
    {
        public Context _context { get; set; }
        public cmd_set_holder(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "设置会长", "绑定会长" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_set_holder";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        async public Task Do(Message msg)
        {
            try
            {
                // 设置会长 [@xxx] [CLUB RID]
                var arr = msg.ExplodeContent;
                if (arr.Length < 3) return;

                // 解析rid
                var rid = arr.Last();
                if (!StringHelper.IsRID(rid)) return;

                var club = _context.ClubsM.FindClub(msg.Self, rid);

                if (club is null)
                {
                    _context.WechatM.SendAtText($"⚠️要设置的俱乐部[{rid}]不存在，请先添加俱乐部。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                // 解析wxid
                var wxid = "";
                var ats = msg.AtWxids;
                if (ats is not null && ats.Length > 0) wxid = ats[0];
                if (string.IsNullOrEmpty(wxid))
                {
                    // 判断是否是wxid
                    if (_context.ContactsM.IsGroupMemberWXID(arr[1], msg.Self, msg.Sender))
                    {
                        wxid = arr[1];
                    }
                    else
                    {
                        var wxids = _context.ContactsM.QueryGroupMemberWXID(arr[1], msg.Self, msg.Sender);

                        // 没有找到成员
                        if (wxids is null)
                        {
                            _context.WechatM.SendAtText($"⚠️在设置会长时没有找到昵称为[{arr[1]}]的群成员。",
                                                       new List<string> { msg.WXID },
                                                       msg.Self,
                                                       msg.Sender);
                            return;
                        }
                        // 找到多个成员
                        else if (wxids.Count > 1)
                        {
                            _context.WechatM.SendAtText($"⚠️在设置会长找到多个可能的群成员，昵称和id如下:\n" +
                                                        $"{string.Join("\n", wxids.Select((a) => $"[{_context.ContactsM.QueryGroupMemberNick(a, msg.Self, msg.Sender)}]{a}"))}\n" +
                                                        $"⚠️请使用以上正确的id重新设置，例如\"设置会长 {wxids[0]} {rid}\"",
                                                        new List<string> { msg.WXID },
                                                        msg.Self,
                                                        msg.Sender);
                            return;
                        }

                        wxid = wxids.First();
                    }
                }

                var nick = _context.ContactsM.QueryGroupMemberNick(wxid, msg.Self, msg.Sender);

                // 缺少用户资料
                var user = _context.ContactsM.FindUser(msg.Self, wxid);
                if (user is null)
                {
                    _context.WechatM.SendAtText($"⚠️唧唧叽缺少[{nick}]的资料，请联系会长使用命令\"刷新群信息\"。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                var curHolder = _context.ContactsM.QueryClubHolder(msg.Self, rid);
                if (!string.IsNullOrEmpty(curHolder))
                {
                    // wxid是否已经是会长
                    if (curHolder == wxid)
                    {
                        _context.WechatM.SendAtText($"[{nick}]已经是俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]的会长。",
                                                    new List<string> { msg.WXID },
                                                    msg.Self,
                                                    msg.Sender);
                        return;
                    }
                    // 这个俱乐部是否已经有会长了
                    else
                    {
                        var curNick = _context.ContactsM.QueryGroupMemberNick(curHolder, msg.Self, msg.Sender);
                        if (string.IsNullOrEmpty(curNick)) curNick = curHolder;
                        _context.WechatM.SendAtText($"⚠️俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]目前的会长是[{curNick}]。\n" +
                                                    $"⚠️若要重新设置会长，请先发送\"取消会长 {rid}\"",
                                                    new List<string> { msg.WXID },
                                                    msg.Self,
                                                    msg.Sender);
                        return;
                    }
                }

                var result = _context.ContactsM.SetHolder(msg.Self, rid, wxid);
                if (result) _context.WechatM.SendAtText($"您已经被指派俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]会长权限。\n" +
                                                        $"若该俱乐部为新加入唧唧叽的俱乐部，现在请前往您的俱乐部群，进行“绑定群”操作。",
                                                        new List<string> { wxid },
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
