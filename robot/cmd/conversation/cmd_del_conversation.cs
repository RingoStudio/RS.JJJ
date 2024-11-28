using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.res.communicate;
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

namespace RS.Snail.JJJ.robot.cmd.conversation
{

    internal class cmd_del_conversation : ICMD
    {
        public Context _context { get; set; }
        public cmd_del_conversation(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "删除对话" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_del_conversation";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        public void Do(Message msg)
        {
            try
            {
                // 删除对话 [OPT:RID] [KEY]
                var rid = "";
                var arr = msg.ExplodeContent;
                var key = "";
                if (arr.Length < 2) return;

                for (int i = 1; i < arr.Length; i++)
                {
                    if (StringHelper.IsRID(arr[i]) && string.IsNullOrEmpty(rid)) rid = arr[i];
                    else if (string.IsNullOrEmpty(key)) key = arr[i];
                }


                // 未指定rid，则为本群rid
                if (string.IsNullOrEmpty(rid))
                {
                    if (msg.Scene == ChatScene.Private) return;
                    else
                    {
                        var group = _context.ContactsM.FindGroup(msg.RoomID);
                        if (group is null)
                        {
                            _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。", new List<string> { msg.Sender }, msg.RoomID);
                            return;
                        }
                        rid = group.RID;
                    }
                }

                if (string.IsNullOrEmpty(rid)) return;

                // 检查本俱乐部权限
                if (_context.ContactsM.QueryRole(msg.Sender, rid: rid) < MinRole)
                {
                    _context.WechatM.SendAtText($"不可以设置其他俱乐部的对话。", new List<string> { msg.Sender }, msg.RoomID);
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

                if (string.IsNullOrEmpty(key))
                {
                    var tip = new List<string>();
                    _context.WechatM.SendAtText($"在删除对话内容时，您输入了空的关键字，删除失败。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                var result = _context.ConversationM.DeleteGroupConversation(rid, key);
                if (result)
                {
                    var desc = $"俱乐部[{_context.ClubsM.QueryClubName(rid) ?? rid}]已经删除了以下对话内容\n" +
                              $"关键字：{key}\n";
                    _context.WechatM.SendAtText(desc, new List<string> { msg.Sender }, msg.RoomID);
                }
                else _context.WechatM.SendAtText("可能是因为关键字不存在，或其他原因，删除对话失败了。", new List<string> { msg.Sender }, msg.RoomID);

            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
