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

    internal class cmd_set_club_drill_use_limit : ICMD
    {
        public Context _context { get; set; }
        public cmd_set_club_drill_use_limit(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "设置钻头达标数量", "设置钻头达标" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_set_club_drill_use_limit";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        public void Do(Message msg)
        {
            try
            {
                // 设置钻头达标 [OPT:RID] [num1] [num2]
                var rid = "";
                var arr = msg.ExplodeContent;
                int max = 0, min = 0, v1 = 0, v2 = 0;
                if (arr.Length > 1)
                {
                    for (int i = 1; i < arr.Length; i++)
                    {
                        if (StringHelper.IsRID(arr[i])) rid = arr[i];
                        if (StringHelper.IsInt(arr[i]))
                        {
                            if (v1 == 0) v1 = Convert.ToInt32(arr[i]);
                            else if (v2 == 0) v2 = Convert.ToInt32(arr[i]);
                        }
                    }
                }

                max = Math.Max(v1, v2);
                min = Math.Min(v1, v2);
                if (max != min && min == 0) min = max;

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

                club.DrillUseLimit1 = max;
                club.DrillUseLimit2 = min;
                var desc = $"已将俱乐部[{club.Name}]的钻头达标数量设置为";
                desc += (max == 0 && min == 0) ? "[不要求]\n之后将不再提醒钻头使用数量不达标成员" :
                                                $"[敢死/苦力{min}, 勘探{max}]\n之后的清资源提醒和钻头提醒将包含钻头使用数量不达标的成员";
                _context.WechatM.SendAtText(desc, new List<string> { msg.Sender }, msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
