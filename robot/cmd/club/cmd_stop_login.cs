using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.game.module;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.cmd.utils;
using RS.Snail.JJJ.robot.include;
using RS.Snail.JJJ.robot.modules;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.club
{

    internal class cmd_stop_login : ICMD
    {
        public Context _context { get; set; }
        public cmd_stop_login(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "停止登录", "终止登录" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_stop_login";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        async public Task Do(Message msg)
        {
            try
            {
                // 停止登录 [OPT:RID/此俱乐部]
                var arr = msg.ExplodeContent;
                var rid = "";
                if (arr.Length > 1) rid = arr[1];

                // 群聊下可设置为
                var role = _context.ContactsM.QueryRole(msg.Self, msg.WXID, msg.Scene == ChatScene.Group ? msg.Sender : "");
                if (rid == "此俱乐部" || rid.ToLower() == "cjlb" || (string.IsNullOrEmpty(rid) && role < UserRole.ADMINISTRATOR))
                {
                    if (msg.Scene == ChatScene.Group) return;
                    var group = _context.ContactsM.FindGroup(msg.Self, msg.Sender);
                    if (group is null) return;
                    rid = group.RID ?? "";
                }
                if (StringHelper.IsRID(rid))
                {
                    if (!_context.ContactsM.CheckGroupRole(msg.Self, rid, msg.WXID, msg.Scene == ChatScene.Group ? msg.Sender : ""))
                    {
                        _context.WechatM.SendAtText($"不可以终止其他俱乐部的登录。",
                                                 new List<string> { msg.WXID },
                                                 msg.Self,
                                                 msg.Sender);
                        return;
                    }
                }
                else rid = "";
                if (role < UserRole.ADMINISTRATOR)
                {
                    if (string.IsNullOrEmpty(rid)) return;
                    if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;
                }

                var result = _context.SnailsM.StopLogin(rid);
                var desc = string.IsNullOrEmpty(rid) ? "[所有俱乐部]" : $"[{_context.ClubsM.QueryClubName(msg.Self, rid) ?? rid}]";
                if (result.result) desc = $"终止{desc}登录成功。";
                else desc = $"终止{desc}登录失败，原因：{result.desc}";

                _context.WechatM.SendAtText(desc,
                                            new List<string> { msg.WXID },
                                            msg.Self,
                                            msg.Sender);

            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
                _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
            }
        }
    }
}
