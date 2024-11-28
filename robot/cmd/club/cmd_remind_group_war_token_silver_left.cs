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

    internal class cmd_remind_group_war_token_silver_left : ICMD
    {
        public Context _context { get; set; }
        public cmd_remind_group_war_token_silver_left(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "提醒清银牌", "提醒清银令牌", "提醒用银牌", "提醒用银令牌" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_remind_group_war_token_silver_left";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        public void Do(Message msg)
        {
            try
            {
                // 提醒金牌 

                var group = _context.ContactsM.FindGroup(msg.RoomID);
                if (group is null)
                {
                    _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }
                var rid = group.RID;

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
                var result = _context.ClubsM.RemindGroupWarTokenSilverLeft(rid, msg.RoomID, msg.Sender);
                if (!result.result)
                {
                    if (string.IsNullOrEmpty(result.desc)) _context.WechatM.SendAtText("⚠️未查询到任何信息。", new List<string> { msg.Sender }, msg.RoomID);
                    else _context.WechatM.SendAtText($"⚠️{result.desc ?? "提醒失败了"}", new List<string> { msg.Sender }, msg.RoomID);
                }
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
