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

    internal class cmd_query_group_war_token_gold_lack : ICMD
    {
        public Context _context { get; set; }
        public cmd_query_group_war_token_gold_lack(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "查询金牌未领", "查询金令牌未领" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_query_group_war_token_gold_lack";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        public void Do(Message msg)
        {
            try
            {
                // 查询物种历史 [OPT:RID]
                var rid = "";
                var arr = msg.ExplodeContent;
                if (arr.Length > 1 && StringHelper.IsRID(arr[1])) rid = arr[1];

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

                var result = _context.ClubsM.QueryGroupWarGoldTokeLack(rid);
                if (string.IsNullOrEmpty(result)) _context.WechatM.SendAtText("⚠️未查询到任何信息。", new List<string> { msg.Sender }, msg.RoomID);
                else if (result.Length > 200)
                {
                    var fileName = $"OUT\\金牌未领查询结果_@{_context.ContactsM.QueryGroupMemberNickForFile(msg.Sender, msg.RoomID)}_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.txt";
                    fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                    System.IO.File.WriteAllText(fileName, result);
                    _context.WechatM.SendFile(fileName, msg.RoomID);
                }
                else _context.WechatM.SendAtText(result, new List<string> { msg.Sender }, msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
