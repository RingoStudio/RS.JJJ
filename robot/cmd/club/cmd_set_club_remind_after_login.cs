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
    
    internal class cmd_set_club_remind_after_login : ICMD
    {
        public Context _context { get; set; }
        public cmd_set_club_remind_after_login(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "设置登录后提醒", "设置自动提醒" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_set_club_remind_after_login";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        async public Task Do(Message msg)
        {
            try
            {
                // 设置登录后提醒 [OPT:RID] [自动/手动]
                var rid = "";
                var arr = msg.ExplodeContent;
                var mode = 0;
                if (arr.Length > 1)
                {
                    for (int i = 1; i <= arr.Length; i++)
                    {
                        if (StringHelper.IsRID(arr[i])) rid = arr[i];
                        if (arr[i] == "开" || arr[i] == "开启" || arr[i].ToLower() == "on") mode = 1;
                        else if (arr[i] == "关" || arr[i] == "关闭" || arr[i].ToLower() == "off") mode = -1;
                    }
                }

                if (mode == 0) return;

                // 未指定rid，则为本群rid
                if (string.IsNullOrEmpty(rid))
                {
                    if (msg.Scene == ChatScene.Private) return;
                    else
                    {
                        var group = _context.ContactsM.FindGroup(msg.Self, msg.Sender);
                        if (group is null)
                        {
                            _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
                                                        new List<string> { msg.WXID },
                                                        msg.Self,
                                                        msg.Sender);
                            return;
                        }
                        rid = group.RID;
                    }
                }

                if (string.IsNullOrEmpty(rid)) return;

                // 检查本俱乐部权限
                if (!_context.ContactsM.CheckGroupRole(msg.Self, rid, msg.WXID, msg.Scene == ChatScene.Group ? msg.Sender : ""))
                {
                    _context.WechatM.SendAtText($"不可以查看其他俱乐部的信息。",
                                             new List<string> { msg.WXID },
                                             msg.Self,
                                             msg.Sender);
                    return;
                }

                // 找到俱乐部
                var club = _context.ClubsM.FindClub(msg.Self, rid);
                if (club is null)
                {
                    _context.WechatM.SendAtText($"⚠️要查询的俱乐部[{rid}]不存在。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;

                club.RemindAfterLogin = (mode > 0);
                var desc = $"已将俱乐部[{club.Name}]的登录后提醒设置为";
                desc += club.RemindAfterLogin ? "[开启]\n下一次登录之后，将自动发出资源提醒" :
                                                "[关闭]\n下一次登录之后，将不会自动发出资源提醒\n但可以手动发出提醒";
                _context.WechatM.SendAtText(desc,
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
