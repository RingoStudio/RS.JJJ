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

    internal class cmd_query_self_info : ICMD
    {
        public Context _context { get; set; }
        public cmd_query_self_info(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "查看我的信息" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_query_self_info";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.NORMAL;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        public void Do(Message msg)
        {
            try
            {
                // 找到群
                var group = _context.ContactsM.FindGroup(msg.RoomID);
                if (group is null)
                {
                    _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请尝试联系超管使用命令\"刷新群信息\"。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                var member = group.Members.ContainsKey(msg.Sender) ? group.Members[msg.Sender] : null;
                if (member is null)
                {
                    _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群内你的资料，请尝试联系会长使用命令\"刷新群信息\"。", new List<string> { msg.Sender }, msg.RoomID);
                }

                // 找到俱乐部
                var rid = group.RID;
                var club = _context.ClubsM.FindClub(rid);
                if (club is null)
                {
                    _context.WechatM.SendAtText($"⚠️当前微信群绑定的俱乐部 [{rid}] 不存在。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;

                var ret = new List<string>();
                ret.Add("为你查到以下信息");
                var role = _context.ContactsM.QueryRole(msg.Sender, msg.RoomID);
                ret.Add($"在本群权限: {include.wechat.UserRoleDesc(role)}");

                if (club is not null)
                {
                    ret.Add($"本群绑定俱乐部: {club.Name} [{club.RID}]");
                    var uids = member?.UIDs ?? new List<string>();
                    if (uids.Count > 0)
                    {
                        ret.Add($"绑定的游戏角色: \n{string.Join("\n", uids.Select((a) => $"{_context.ClubsM.QueryMemberName(a)} [{a}]"))}");
                    }
                    else
                    {
                        if (role <= UserRole.GROUP_HOLDER)
                        {
                            ret.Add($"绑定的游戏角色: (未绑定)");
                        }
                    }
                }

                if (ret is null || ret.Count == 0)
                {
                    _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }
                var result = string.Join("\n", ret);
                if (result.Length > 200)
                {
                    var fileName = $"OUT\\我的信息查询结果_@{_context.ContactsM.QueryGroupMemberNickForFile(msg.Sender, msg.RoomID)}_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.txt";
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
