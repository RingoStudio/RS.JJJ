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
    
    internal class cmd_remind_group_war_mine : ICMD
    {
        public Context _context { get; set; }
        public cmd_remind_group_war_mine(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "提醒挖矿" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_remind_group_war_mine";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        async public Task Do(Message msg)
        {
            try
            {
                // 提醒挖矿 [OPT:NEAR]
                var arr = msg.ExplodeContent;
                double near = 0;
                if (arr.Length > 1 && StringHelper.IsNumeric(arr[1])) near = Convert.ToDouble(arr[1]);
                near = Math.Min(100, Math.Max(0.5, near));

                // 未指定rid，则为本群rid

                var group = _context.ContactsM.FindGroup(msg.Self, msg.Sender);
                if (group is null)
                {
                    _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }
                var rid = group.RID;

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

                await Task.Run(() =>
                {
                    var result = _context.ClubsM.RemindGroupWarMine(msg.Self, rid, msg.Sender, msg.WXID, near);
                    if (!result.result)
                    {
                        if (string.IsNullOrEmpty(result.desc)) _context.WechatM.SendAtText("⚠️未查询到任何信息。",
                                                                                        new List<string> { msg.WXID },
                                                                                        msg.Self,
                                                                                        msg.Sender);
                        else _context.WechatM.SendAtText($"⚠️{result.result}",
                                                        new List<string> { msg.WXID },
                                                        msg.Self,
                                                        msg.Sender);
                    }
                });
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
            }
        }
    }
}
