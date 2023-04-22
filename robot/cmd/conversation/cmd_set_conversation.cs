using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.res.communicate;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.conversation
{
    [attribute.CmdClass]
    internal class cmd_set_conversation
    {
        public const string Instrus = "更新对话";
        public const string Tag = "cmd_set_conversation";
        public const ChatScene EnableScene = ChatScene.All;
        public const UserRole MinRole = UserRole.GROUP_MANAGER;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;
        public const string _checkTag = "cmd_set_conversation_check";
        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                context.CommunicateM.UnregistWaitMessageRequest(msg.Self, msg.Sender, msg.WXID, _checkTag);
                // 更新对话 [OPT:RID] [KEY]
                var rid = "";
                var arr = msg.ExplodeContent;
                var key = "";
                if (arr.Length < 3) return;

                for (int i = 1; i <= arr.Length; i++)
                {
                    if (StringHelper.IsRID(arr[i])) rid = arr[i];
                    else if (string.IsNullOrEmpty(key)) key = arr[i];
                }


                // 未指定rid，则为本群rid
                if (string.IsNullOrEmpty(rid))
                {
                    if (msg.Scene == ChatScene.Private) return;
                    else
                    {
                        var group = context.ContactsM.FindGroup(msg.Self, msg.Sender);
                        if (group is null)
                        {
                            context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
                                                        new List<string> { msg.WXID },
                                                        msg.Self,
                                                        msg.Sender);
                            return;
                        }
                        rid = group.RID;
                    }
                }

                if (string.IsNullOrEmpty(rid)) return;

                // 检查订购
                var purchase = context.PurchaseM.CheckPurchase(rid, msg);
                if (!purchase.result)
                {
                    if (!string.IsNullOrEmpty(purchase.desc))
                    {
                        context.WechatM.SendAtText(purchase.desc,
                                              new List<string> { msg.WXID },
                                              msg.Self,
                                              msg.Sender);
                    }
                    return;
                }

                // 检查本俱乐部权限
                if (!context.ContactsM.CheckGroupRole(msg.Self, rid, msg.WXID, msg.Scene == ChatScene.Group ? msg.Sender : ""))
                {
                    context.WechatM.SendAtText($"不可以设置其他俱乐部的对话。",
                                             new List<string> { msg.WXID },
                                             msg.Self,
                                             msg.Sender);
                    return;
                }

                // 找到俱乐部
                var club = context.ClubsM.FindClub(msg.Self, rid);
                if (club is null)
                {
                    context.WechatM.SendAtText($"⚠️要查询的俱乐部[{rid}]不存在。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }


                if (string.IsNullOrEmpty(key))
                {
                    var tip = new List<string>();
                    context.WechatM.SendAtText($"在设置对话内容时，您输入了空的关键字，设置失败。",
                                                 new List<string> { msg.WXID },
                                                 msg.Self,
                                                 msg.Sender);
                    return;
                }

                context.WechatM.SendAtText($"现在请你为关键字[{key}]指定要回复的内容。\n" +
                                           $"可接受的回复内容包括：文本(300字以内)，图片(1个，格式为.png/.jpg等)，文档(1个，格式为.docx/.xlsx/.pptx/.pdf等)\n" +
                                           $"若回复内容满足以上要求，将被直接保存\n" +
                                           $"请在20秒之内发出，或回复\"取消\"",
                                              new List<string> { msg.WXID },
                                              msg.Self,
                                              msg.Sender);

            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
            }
        }
        private static void Loops(Context context, Message msg, string rid, string key)
        {

            try
            {
                context.CommunicateM.RegistWaitMessageRequest(msg.Self, msg.Sender, msg.WXID,
            new Action<Message>((_msg) =>
                                                    {
                                                        try
                                                        {
                                                            if (_msg.Type == Tools.Common.Enums.WechatMessageType.Text)
                                                            {
                                                                if (_msg.Content == "取消") return;
                                                                else
                                                                {
                                                                    var text = _msg.Content.Trim();
                                                                    if (text.Length >= 300)
                                                                    {
                                                                        context.WechatM.SendAtText("文本长度超过300，请重新发送。\n" +
                                                                                                   "发送\"取消\"停止操作。\n" +
                                                                                                   "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                                                                   new List<string> { _msg.WXID },
                                                                                                   _msg.Self,
                                                                                                   _msg.Sender);
                                                                        Loops(context, _msg, rid, key);
                                                                    }
                                                                    else if(string.IsNullOrEmpty(_msg.Content.Trim()))
                                                                    {
                                                                        context.WechatM.SendAtText("你输入了空的文本，请重新发送。\n" +
                                                                                                   "发送\"取消\"停止操作。\n" +
                                                                                                   "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                                                                   new List<string> { _msg.WXID },
                                                                                                   _msg.Self,
                                                                                                   _msg.Sender);
                                                                        Loops(context, _msg, rid, key);
                                                                    }
                                                                    else
                                                                    {
                                                                        SaveConversation(context, _msg, key, rid, _msg.Content.Trim());
                                                                    }
                                                                }
                                                            }
                                                            else if (_msg.Type == Tools.Common.Enums.WechatMessageType.File)
                                                            {

                                                                var path = context.WechatM.GetFilePath(_msg);
                                                                if (!System.IO.File.Exists(path))
                                                                {
                                                                    context.WechatM.SendAtText($"附件接收失败，请重新发送。\n" +
                                                                                               "发送\"取消\"停止操作。\n" +
                                                                                               "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                                                            new List<string> { _msg.WXID },
                                                                                            _msg.Self,
                                                                                            _msg.Sender);
                                                                    Loops(context, _msg, rid, key);
                                                                }
                                                                else
                                                                {
                                                                    SaveConversation(context, _msg, key, rid, $"__file::{path}");
                                                                }

                                                            }
                                                            else if (_msg.Type == Tools.Common.Enums.WechatMessageType.Image)
                                                            {
                                                                var path = context.WechatM.GetImagePath(_msg);
                                                                if (!System.IO.File.Exists(path))
                                                                {
                                                                    context.WechatM.SendAtText($"附件接收失败，请重新发送。\n" +
                                                                                           "发送\"取消\"停止操作。\n" +
                                                                                           "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                                                        new List<string> { _msg.WXID },
                                                                                        _msg.Self,
                                                                                        _msg.Sender);
                                                                    Loops(context, _msg, rid, key);
                                                                }
                                                                else
                                                                {
                                                                    SaveConversation(context, _msg, key, rid, $"__image::{path}");
                                                                }
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Context.Logger.Write(ex, Tag);
                                                            context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。",
                                                                                     new List<string> { _msg.WXID },
                                                                                     _msg.Self,
                                                                                     _msg.Sender);
                                                        }
                                                    }),
                                                    null,
                                                    null,
                                                    20,
                                                    _checkTag);
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
                context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。",
                                            new List<string> { msg.WXID },
                                            msg.Self,
                                            msg.Sender);
            }
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="texts"></param>
        /// <param name="images"></param>
        /// <param name="files"></param>
        private static void SaveConversation(Context context, Message msg, string rid, string key, string content)
        {
            try
            {
                var result = context.ConversationM.UpdateGroupConversation(rid, key, content);
                if (result)
                {
                    var desc = $"俱乐部[{context.ClubsM.QueryClubName(msg.Self, rid) ?? rid}]已经更新了新的对话内容\n" +
                          $"关键字：{key}\n" +
                          $"回复内容：";
                    if (content.StartsWith("__file::")) desc += $"(文件){IOHelper.GetFileName(content.Substring(8))}";
                    else if (content.StartsWith("__image::")) desc += $"(图片)";
                    else desc += $"(文本){StringHelper.GetEllipsisText(content, 50)}";
                    context.WechatM.SendAtText(desc,
                                               new List<string> { msg.WXID },
                                               msg.Self,
                                               msg.Sender);
                }
                else context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
                context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。",
                                            new List<string> { msg.WXID },
                                            msg.Self,
                                            msg.Sender);
            }
        }
    }
}
