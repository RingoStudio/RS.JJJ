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
    internal class cmd_auto_bind : ICMD
    {
        public Context _context { get; set; }
        public cmd_auto_bind(Context context)
        {
            _context = context;
        }
        public List<string> Commands { get; } = new List<string> { "批量绑定成员", "自动绑定成员" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag { get; } = "cmd_auto_bind";
        public ChatScene EnableScene { get; } = include.ChatScene.Group;
        public UserRole MinRole { get; } = include.UserRole.GROUP_HOLDER;
        public WechatMessageType AcceptMessageType { get; } = Tools.Common.Enums.WechatMessageType.Text;

        public void Do(Message msg)
        {
            try
            {
                // 找到群
                var group = _context.ContactsM.FindGroup(msg.RoomID);
                if (group is null)
                {
                    _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
                                                new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                // 找到俱乐部
                var rid = group.RID;
                var club = _context.ClubsM.FindClub(rid);
                if (club is null)
                {
                    _context.WechatM.SendAtText($"⚠️当前微信群绑定的俱乐部 [{rid}] 不存在。",
                                                new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;

                if (club.Members.Count < 20)
                {
                    _context.WechatM.SendAtText($"⚠️当前微信群绑定的俱乐部 [{club.Name} {rid}] 成员数量异常。\n" +
                                                $"请重新登录更新成员数据后再试。",
                                                new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }


                var ret = new List<string>();
                var emptyUIDs = club.Members.DeepCopy();
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

                emptyUIDs = emptyUIDs.Except(existUIDs).ToList();

                foreach (var uid in emptyUIDs)
                {
                    if (uid == "821ZFDRJLA8P40")
                    {
                        var a = 0;
                    }
                    var uidTimes = 0;
                    var uidConnect = "";
                    var gameNick = _context.ClubsM.QueryMemberName(uid);
                    if (string.IsNullOrEmpty(gameNick)) continue;
                    foreach (var member in group.Members)
                    {
                        if (member.Value.UIDs is not null && member.Value.UIDs.Count > 0) continue;
                        if (string.IsNullOrEmpty(member.Value.NickName)) continue;
                        if (member.Value.NickName.Contains(gameNick))
                        {
                            uidTimes++;
                            uidConnect = member.Key;
                        }
                    }

                    if (uidTimes == 1 && !string.IsNullOrEmpty(uidConnect))
                    {
                        var flag = _context.ContactsM.SetMember(msg.RoomID, uidConnect, uid);
                        if (flag) ret.Add($"@{group.Members[uidConnect].NickName} → {gameNick}[{uid}]");
                    }
                }

                var result = "";
                if (ret.Count > 0) result = $"成功绑定 {ret.Count} 个游戏角色: \n" + string.Join("\n", ret);
                else result = "没有找到任何有效成员绑定。";

                if (result.Length > 200)
                {
                    var fileName = $"OUT\\自动绑定成员结果_@{_context.ContactsM.QueryGroupMemberNickForFile(msg.Sender, msg.RoomID)}_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.txt";
                    fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                    System.IO.File.WriteAllText(fileName, result);
                    _context.WechatM.SendFile(fileName, msg.RoomID);
                }
                else _context.WechatM.SendAtText(result, new List<string> { msg.Sender }, msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
                _context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。", new List<string> { msg.Sender }, msg.RoomID);
            }
        }
    }
}