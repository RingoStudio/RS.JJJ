﻿using RS.Snail.JJJ.boot;
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

    internal class cmd_auto_clear_unexist_bindings : ICMD
    {
        public Context _context { get; set; }
        public cmd_auto_clear_unexist_bindings(Context context)
        {
            _context = context;
        }
        public List<string> Commands { get; } = new List<string> { "清理成员绑定", "清理失效绑定", "清理失效成员绑定" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag { get; } = "cmd_auto_clear_unexist_bindings";
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

                foreach (var member in group.Members)
                {
                    if (member.Value.UIDs is not null && member.Value.UIDs.Count > 0)
                    {
                        member.Value.UIDs = member.Value.UIDs.Where((uid) =>
                        {
                            var flag = club.Members.Contains(uid);
                            if (!flag) ret.Add($"@{member.Value.NickName} -> {_context.ClubsM.QueryMemberName(uid) ?? ""}[{uid}]");
                            return flag;
                        }).ToList();

                        if (member.Value.UIDs.Count <= 0 && member.Value.Role < include.UserRole.GROUP_MANAGER)
                        {
                            member.Value.Role = include.UserRole.NONE;
                            _context.ContactsM.SetRoleCache(include.UserRole.NONE, member.Key, msg.RoomID);
                        }
                    }
                }

                var result = "";
                if (ret.Count > 0) result = $"成功解除 {ret.Count} 条失效成员绑定: \n" + string.Join("\n", ret);
                else result = "没有找到任何失效成员绑定。";

                _context.WechatM.SendAtText(result, new List<string> { msg.Sender }, msg.RoomID);

            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
                _context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。",
                                            new List<string> { msg.Sender }, msg.RoomID);
            }
        }
    }
}
