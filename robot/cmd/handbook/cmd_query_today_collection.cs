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

    internal class cmd_query_today_collection : ICMD
    {
        public Context _context { get; set; }
        public cmd_query_today_collection(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "今日贵重" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_query_today_collection";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.NORMAL;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        public void Do(Message msg)
        {
            try
            {
                if (_context.ConfigsM.SwitchHandbookClose) return;

                if (!_context.ContactsM.IsAdmin(msg.Sender))
                {
                    var group = _context.ContactsM.FindGroup(msg.RoomID);
                    if (group is null)
                    {
                        _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。", new List<string> { msg.Sender }, msg.RoomID);
                        return;
                    }
                    var rid = group.RID;
                    if (string.IsNullOrEmpty(rid)) return;

                    // 找到俱乐部
                    var club = _context.ClubsM.FindClub(rid);
                    if (club is null)
                    {
                        _context.WechatM.SendAtText($"⚠️俱乐部[{rid}]不存在。", new List<string> { msg.Sender }, msg.RoomID);
                        return;
                    }

                    // 检查订购
                    if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;
                }

                if (!_context.HandbookM.CheckCD(msg.Sender)) return;

                var result = _context.HandbookM.QueryTodayCollectionHandbook();
                if (result.Count == 0)
                {
                    _context.WechatM.SendAtText("没有找到相关贵重品!", new List<string> { msg.Sender }, msg.RoomID);
                }
                else
                {
                    foreach (var path in result)
                    {
                        _context.WechatM.SendImage(path, msg.RoomID);
                    }
                }
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}