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

    internal class cmd_set_club_remind_mine_close : ICMD
    {
        public Context _context { get; set; }
        public cmd_set_club_remind_mine_close(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "设置提醒挖矿即将超时", "设置提醒挖矿即将到时" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_set_club_remind_mine_close";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        async public Task Do(Message msg)
        {
            try
            {
                // 设置北极贝需要买卷轴 [OPT:RID] [是/否]
                var rid = "";
                var arr = msg.ExplodeContent;
                var mode = 0;
                if (arr.Length > 1)
                {
                    for (int i = 1; i <= arr.Length; i++)
                    {
                        if (StringHelper.IsRID(arr[i])) rid = arr[i];
                        if (arr[i] == "开" || arr[i] == "开启" || arr[i].ToLower() == "on" || arr[i].ToLower() == "是" || arr[i].ToLower() == "需要") mode = 1;
                        else if (arr[i] == "关" || arr[i] == "关闭" || arr[i].ToLower() == "off" || arr[i].ToLower() == "否" || arr[i].ToLower() == "不需要") mode = -1;
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

                club.DontRemindMineClose = (mode < 0);
                var desc = $"已将俱乐部[{club.Name}]的提醒成员挖矿即将到时设置为";
                desc += (!club.DontRemindMineClose) ? "[需要]\n手动或自动提醒时，将会提醒成员挖矿半小时内到期以及睡前换矿" :
                                               "[不需要]\n手动或自动提醒时，将不会提醒成员挖矿半小时内到期以及睡前换矿";
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
