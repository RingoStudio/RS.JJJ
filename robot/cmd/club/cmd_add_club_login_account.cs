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
    internal class cmd_add_club_login_account : ICMD
    {
        public Context _context { get; set; }
        public cmd_add_club_login_account(Context context)
        {
            _context = context;
        }
        public List<string> Commands { get; } = new List<string> { "增加俱乐部登录账号", "更新俱乐部登录账号" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag { get; } = "cmd_add_club_login_account";
        public ChatScene EnableScene { get; } = include.ChatScene.All;
        public UserRole MinRole { get; } = include.UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType { get; } = Tools.Common.Enums.WechatMessageType.Text;

        public void Do(Message msg)
        {
            try
            {
                // 增加俱乐部登录账号 [RID] [ACCOUNT] [PASSWORD]
                var arr = msg.ExplodeContent;
                if (arr.Length < 4) return;

                var rid = arr[1];
                if (!StringHelper.IsRID(rid)) return;
                var account = arr[2];
                var password = arr[3];

                // 检查专有权限
                if (_context.ContactsM.QueryRole(msg.Sender, rid: rid) < MinRole)
                {
                    _context.WechatM.SendAtText($"您没有设置该俱乐部的登录账号密码的权限。",
                                             new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                // 检查俱乐部是否存在
                var club = _context.ClubsM.FindClub(rid);
                if (club is null)
                {
                    _context.WechatM.SendAtText($"未找到俱乐部[{rid}]。",
                                                new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                // 检查订阅
                if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;

                // 执行
                var result = _context.ClubsM.AddClubLoginAccount(rid, account, password);
                if (!result) _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
                                                         new List<string> { msg.Sender }, msg.RoomID);
                else _context.WechatM.SendAtText($"⚠️已成功更新俱乐部 [{club.Name} {rid}]的账号密码。",
                                                 new List<string> { msg.Sender }, msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
                _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
                                             new List<string> { msg.Sender }, msg.RoomID);
            }
        }
    }
}
