using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.cmd.utils;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.wechat
{

    internal class cmd_refresh_contacts : ICMD
    {
        public Context _context { get; set; }
        public cmd_refresh_contacts(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "刷新群信息", "刷新联系人" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_refresh_contacts";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.GROUP_HOLDER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        public void Do(Message msg)
        {
            try
            {
                var arr = msg.ExplodeContent;
                var isAll = false;

                if (msg.Scene == include.ChatScene.Private) isAll = true;
                else if (arr.Length > 1)
                {
                    switch (arr[1].ToLower())
                    {
                        case "all":
                        case "全部":
                        case "所有":
                            isAll = true;
                            break;
                        default:
                            isAll = false;
                            break;
                    }
                }


                //var role = _context.ContactsM.QueryRole(msg.Sender, msg.RoomID);
                var isAdmin = _context.ContactsM.IsAdmin(msg.Sender);
                if (!isAdmin)
                {
                    if (msg.Scene != include.ChatScene.Group)
                    {
                        //context.WechatM.SendAtText($"你只能在自己的群里使用这个命令。",
                        //                            new List<string> { msg.WXID },
                        //                            msg.Self,
                        //                            msg.Sender);
                        return;
                    }
                    isAll = false;
                }
                if (!isAll)
                {
                    var group = _context.ContactsM.FindGroup(msg.RoomID);

                    if (group is null)
                    {
                        _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。", new List<string> { msg.Sender }, msg.RoomID);
                        return;
                    }
                    var rid = group.RID;
                    if (!StringHelper.IsRID(rid)) return;

                    if (!isAdmin)
                    {
                        // 检查本俱乐部权限
                        if (_context.ContactsM.QueryRole(msg.Sender, rid: rid) < MinRole)
                        {
                            _context.WechatM.SendAtText($"不可以更新其他俱乐部微信群的信息。", new List<string> { msg.Sender }, msg.RoomID);
                            return;
                        }

                        if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;
                    }
                }

                var result = _context.WechatM.RefreshGroupMemberNames(isAll ? "" : msg.RoomID);
                var desc = isAll ? "所有群" : (_context.ContactsM.FindGroup(msg.RoomID)?.Name ?? "指定" + "群");

                if (result) _context.WechatM.SendAtText($"刷新{desc}信息成功。", new List<string> { msg.Sender }, msg.RoomID);
                else _context.WechatM.SendAtText("因未知原因，操作失败了。", new List<string> { msg.Sender }, msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
