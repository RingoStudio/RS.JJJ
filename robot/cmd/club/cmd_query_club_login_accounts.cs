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

    internal class cmd_query_club_login_accounts : ICMD
    {
        public Context _context { get; set; }
        public cmd_query_club_login_accounts(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "查询俱乐部登录账号" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_query_club_login_accounts";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        public void Do(Message msg)
        {
            try
            {
                var rid = "";
                var arr = msg.ExplodeContent;
                if (arr.Length > 1) rid = arr[1];
                if (msg.Scene == ChatScene.Private && string.IsNullOrEmpty(rid)) return;

                if (msg.Scene == ChatScene.Group && string.IsNullOrEmpty(rid))
                {
                    var group = _context.ContactsM.FindGroup(msg.RoomID);
                    if (group is null) return;

                    rid = group.RID;
                    if (string.IsNullOrEmpty(rid)) return;
                }


                if (_context.ContactsM.QueryRole(msg.Sender, rid: rid) < MinRole)
                {
                    _context.WechatM.SendAtText($"不可以查看其他俱乐部的登录账号。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                var club = _context.ClubsM.FindClub(rid);
                if (club is null)
                {
                    _context.WechatM.SendAtText("没有找到这个俱乐部。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;

                _context.WechatM.SendAtText(club.LoginAccountDesc(), new List<string> { msg.Sender }, msg.RoomID);

            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
                _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。", new List<string> { msg.Sender }, msg.RoomID);
            }
        }
    }
}
