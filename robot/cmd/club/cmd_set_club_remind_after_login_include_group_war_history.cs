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

    internal class cmd_set_club_remind_after_login_include_group_war_history : ICMD
    {
        public Context _context { get; set; }
        public cmd_set_club_remind_after_login_include_group_war_history(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "设置登录后提醒物种历史", "设置自动提醒物种历史" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_set_club_remind_after_login_include_group_war_history";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        public void Do(Message msg)
        {
            try
            {
                // 设置登录后提醒 [OPT:RID] [自动/手动]
                var rid = "";
                var arr = msg.ExplodeContent;
                var mode = 0;
                if (arr.Length > 1)
                {
                    for (int i = 1; i < arr.Length; i++)
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
                        var group = _context.ContactsM.FindGroup(msg.RoomID);
                        if (group is null)
                        {
                            _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。", new List<string> { msg.Sender }, msg.RoomID);
                            return;
                        }
                        rid = group.RID;
                    }
                }

                if (string.IsNullOrEmpty(rid)) return;

                // 检查本俱乐部权限
                if (_context.ContactsM.QueryRole(msg.Sender, rid: rid) < MinRole)
                {
                    _context.WechatM.SendAtText($"您没有查看该俱乐部相关信息的权限。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                // 找到俱乐部
                var club = _context.ClubsM.FindClub(rid);
                if (club is null)
                {
                    _context.WechatM.SendAtText($"⚠️要查询的俱乐部[{rid}]不存在。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;

                club.RemindContentContainsGroupWarHistory = (mode > 0);
                var desc = $"已将俱乐部[{club.Name}]的登录后提醒物种历史设置为";
                if (club.RemindAfterLogin)
                {
                    desc += club.RemindContentContainsGroupWarHistory ? "[开启]\n下一次登录之后，提醒内容中将包括物种历史" :
                                                                        "[关闭]\n下一次登录之后，提醒内容中将不包括物种历史";
                }
                else
                {
                    desc += club.RemindContentContainsGroupWarHistory ? "[开启]\n需先开启\"登录后提醒\"，下一次登录之后方可提醒内容中将包括物种历史" :
                                                                        "[关闭]\n需先开启\"登录后提醒\"，下一次登录之后方可提醒内容中将不包括物种历史";
                }

                _context.WechatM.SendAtText(desc, new List<string> { msg.Sender }, msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
