using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.cmd.utils;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Enums;
using RS.Tools.Common.extension;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.club
{

    internal class cmd_query_not_binded : ICMD
    {
        public Context _context { get; set; }
        public cmd_query_not_binded(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "查看未绑定成员" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_query_not_binded";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        async public Task Do(Message msg)
        {
            try
            {
                // 找到群
                var group = _context.ContactsM.FindGroup(msg.Self, msg.Sender);
                if (group is null)
                {
                    _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                // 找到俱乐部
                var rid = group.RID;
                var club = _context.ClubsM.FindClub(msg.Self, rid);
                if (club is null)
                {
                    _context.WechatM.SendAtText($"⚠️当前微信群绑定的俱乐部 [{rid}] 不存在。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;

                if (club.Members.Count < 20)
                {
                    _context.WechatM.SendAtText($"⚠️当前微信群绑定的俱乐部 [{club.Name} {rid}] 成员数量异常。\n" +
                                               $"请重新登录更新成员数据后再试。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                await Task.Run(() =>
                {
                    var existUIDs = new List<string>();

                    foreach (var member in group.Members)
                    {
                        if (member.Value.UIDs is not null)
                        {
                            foreach (var uid in member.Value.UIDs)
                            {
                                if (!existUIDs.Contains(uid)) existUIDs.Add(uid);
                            }
                        }
                    }

                    var ret = club.Members.DeepCopy()
                             .Except(existUIDs)
                             .ToList()
                             .Select((uid) => $"[{_context.ClubsM.QueryMemberName(msg.Self, uid)}] {uid}")
                             .ToList();

                    var result = "";
                    if (ret.Count > 20) result = $"共找到 {ret.Count} 个未绑定游戏角色, 请发送\"查询成员总览\"查看详情。";
                    else if (ret.Count > 0) result = $"共找到 {ret.Count} 个未绑定游戏角色 ([昵称] UID): \n" + string.Join("\n", ret);
                    else result = "大家都绑定好啦~";

                    _context.WechatM.SendAtText(result,
                                               new List<string> { msg.WXID },
                                               msg.Self,
                                               msg.Sender);
                });
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
                _context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
            }
        }
    }
}
