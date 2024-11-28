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
    
    internal class cmd_set_club_login_auto : ICMD
    {
        public Context _context { get; set; }
        public cmd_set_club_login_auto(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "设置自动登录", "设置登录模式" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_set_club_login_auto";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        public void Do(Message msg)
        {
            try
            {
                // 设置登录模式 [OPT:RID] [自动/手动]
                var rid = "";
                var arr = msg.ExplodeContent;
                var auto = 0;
                if (arr.Length > 1)
                {
                    for (int i = 1; i < arr.Length; i++)
                    {
                        if (StringHelper.IsRID(arr[i])) rid = arr[i];
                        if (arr[i] == "自动" || arr[i] == "开" || arr[i] == "开启" || arr[i].ToLower() == "on") auto = 1;
                        else if (arr[i] == "手动" || arr[i] == "关" || arr[i] == "关闭" || arr[i].ToLower() == "off") auto = -1;
                    }
                }

                if (auto == 0) return;

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

                club.LoginAuto = (auto > 0);
                var desc = $"已将俱乐部[{club.Name}]的自动登录设置为";
                desc += club.LoginAuto ? "[开启]\n该俱乐部将在指定时间点进行自动登录\n或可以在任何时间手动登录" :
                                         "[关闭]\n该俱乐部将不会参与自动登录\n但可以在任何时间手动登录";
                _context.WechatM.SendAtText(desc, new List<string> { msg.Sender }, msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
