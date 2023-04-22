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
    internal class cmd_set_conversation_global
    {
        public const string Instrus = "更新全局对话";
        public const string Tag = "cmd_set_conversation_global";
        public const ChatScene EnableScene = ChatScene.All;
        public const UserRole MinRole = UserRole.ADMINISTRATOR;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;
        public const string _checkTag = "cmd_set_conversation_globalcheck";
        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                context.CommunicateM.UnregistWaitMessageRequest(msg.Self, msg.Sender, msg.WXID, _checkTag);
                // 更新对话 [KEY]
                var rid = "";
                var arr = msg.ExplodeContent;
                var key = "";
                if (arr.Length < 2) return;

                key = arr[1];

                if (string.IsNullOrEmpty(key))
                {
                    var tip = new List<string>();
                    if (string.IsNullOrEmpty(key)) tip.Add("空的关键字");
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
        private static void Loops(Context context, Message msg, string key)
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
                                                                        Loops(context, _msg, key);
                                                                    }
                                                                    else if (string.IsNullOrEmpty(_msg.Content.Trim()))
                                                                    {
                                                                        context.WechatM.SendAtText("你输入了空的文本，请重新发送。\n" +
                                                                                                   "发送\"取消\"停止操作。\n" +
                                                                                                   "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                                                                   new List<string> { _msg.WXID },
                                                                                                   _msg.Self,
                                                                                                   _msg.Sender);
                                                                        Loops(context, _msg, key);
                                                                    }
                                                                    else
                                                                    {
                                                                        SaveConversation(context, _msg, key, _msg.Content.Trim());
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
                                                                    Loops(context, _msg, key);
                                                                }
                                                                else
                                                                {
                                                                    SaveConversation(context, _msg, key, $"__file::{path}");
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
                                                                    Loops(context, _msg, key);
                                                                }
                                                                else
                                                                {
                                                                    SaveConversation(context, _msg, key, $"__image::{path}");
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
        private static void SaveConversation(Context context, Message msg, string key, string content)
        {
            try
            {
                var result = context.ConversationM.UpdateGlobalConversation(key, content);
                if (result)
                {
                    var desc = $"已经更新了新的全局对话内容\n" +
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
