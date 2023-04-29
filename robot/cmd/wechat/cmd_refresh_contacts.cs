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

        async public Task Do(Message msg)
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


                var role = _context.ContactsM.QueryRole(msg.Self, msg.WXID, msg.Sender);
                if (role < include.UserRole.ADMINISTRATOR)
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

                await Task.Run(() =>
                {
                    if (!isAll)
                    {
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
                        if (!StringHelper.IsRID(rid)) return;

                        if (role < UserRole.ADMINISTRATOR)
                        {
                            // 检查本俱乐部权限
                            if (!_context.ContactsM.CheckGroupRole(msg.Self, rid, msg.WXID, msg.Scene == ChatScene.Group ? msg.Sender : ""))
                            {
                                _context.WechatM.SendAtText($"不可以更新其他俱乐部微信群的信息。",
                                                         new List<string> { msg.WXID },
                                                         msg.Self,
                                                         msg.Sender);
                                return;
                            }

                            if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;
                        }
                    }

                    var result = _context.WechatM.RefreshGroupMemberNames(msg.Self, isAll ? "" : msg.Sender);
                    var desc = isAll ? "通讯录" : (_context.ContactsM.FindGroup(msg.Self, msg.Sender)?.Name ?? "" + "群");

                    if (result) _context.WechatM.SendAtText($"刷新{desc}信息成功。",
                                                           new List<string> { msg.WXID },
                                                           msg.Self,
                                                           msg.Sender);
                    else _context.WechatM.SendAtText("因未知原因，操作失败了。",
                                                    new List<string> { msg.WXID },
                                                    msg.Self,
                                                    msg.Sender);
                });
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
            }
        }
    }
}
