using Org.BouncyCastle.Utilities;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.misc
{
    [attribute.CmdClass]
    internal class cmd_broadcast_global
    {
        public const string Instrus = "全局广播";
        public const string Tag = "cmd_broadcast_global";
        public const include.ChatScene EnableScene = include.ChatScene.All;
        public const include.UserRole MinRole = include.UserRole.ADMINISTRATOR;
        public const RS.Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;
        private static string _continueTag = "cmd_broadcast_continue";
        private static int _maxCount = 1;
        private static int _maxLength = 300;
        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                context.CommunicateM.UnregistWaitMessageRequest(msg.Self, msg.Sender, msg.WXID, _continueTag);
                var arr = msg.ExplodeContent;
                if (arr.Length < 2) return;
                var instru = arr.First();
                var texts = new List<string> { msg.Content.Substring(instru.Length).Trim() };
                var images = new List<string>();
                var files = new List<string>();

                context.WechatM.SendAtText("请发送\"确定\"立即向本服务器上所有唧唧叽的所有群发出以上广播内容。\n" +
                                           "若想要添加文本/文件/图片，请在此发送。\n" +
                                           $"你最多可以发送不超过{_maxLength}字的文本，和最多{_maxCount}个附件。\n" +
                                           "若想终止以上广播，请发送\"取消\"。\n" +
                                           "以上操作20秒内有效，超时未回复将自动终止发送。",
                                           new List<string> { msg.WXID },
                                           msg.Self,
                                           msg.Sender);
                Loops(context, msg, texts, images, files);
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
            }
        }

        private static string AttackDesc(int textLength, int attachCount)
        {
            var descs = new List<string>();
            if (textLength < _maxLength) descs.Add($"文本({textLength}/{_maxLength})");
            if (attachCount < _maxCount) descs.Add($"图片文件({attachCount}/{_maxCount})");
            if (descs.Count > 0) return $"你可以继续添加 {string.Join("和", descs)}，请在此发送。\n";
            else return "";
        }
        /// <summary>
        /// 进入附件接收循环
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="texts"></param>
        /// <param name="images"></param>
        /// <param name="files"></param>
        private static void Loops(Context context, Message msg, List<string> texts, List<string> images, List<string> files)
        {

            try
            {
                context.CommunicateM.RegistWaitMessageRequest(msg.Self, msg.Sender, msg.WXID,
                                                            new Action<Message>((_msg) =>
                                                            {
                                                                try
                                                                {
                                                                    var attachCount = files.Count + images.Count;
                                                                    var textLength = texts.Select((a) => a.Length).Sum();
                                                                    if (_msg.Type == Tools.Common.Enums.WechatMessageType.Text)
                                                                    {
                                                                        if (_msg.Content == "取消") return;
                                                                        else if (_msg.Content == "确定")
                                                                        {
                                                                            SendBroadCasts(context, _msg, texts, images, files);
                                                                            return;
                                                                        }
                                                                        else
                                                                        {
                                                                            var text = _msg.Content.Trim();
                                                                            if (textLength + text.Length >= 500)
                                                                            {
                                                                                context.WechatM.SendAtText("因文本长度超长，内容增加失败。\n" +
                                                                                                        "请发送\"确定\"立即发出以上广播内容。\n" +
                                                                                                        AttackDesc(textLength, attachCount) +
                                                                                                        "若想终止以上广播，请发送\"取消\"。\n" +
                                                                                                        "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                                                                        new List<string> { _msg.WXID },
                                                                                                        _msg.Self,
                                                                                                        _msg.Sender);
                                                                                Loops(context, _msg, texts, images, files);
                                                                            }
                                                                            else
                                                                            {
                                                                                texts.Add(text);
                                                                                context.WechatM.SendAtText("成功接收文本。\n" +
                                                                                                        "请发送\"确定\"立即发出以上广播内容。\n" +
                                                                                                        AttackDesc(textLength, attachCount) +
                                                                                                        "若想终止以上广播，请发送\"取消\"。\n" +
                                                                                                        "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                                                                        new List<string> { _msg.WXID },
                                                                                                        _msg.Self,
                                                                                                        _msg.Sender);
                                                                                Loops(context, _msg, texts, images, files);
                                                                            }
                                                                        }
                                                                    }
                                                                    else if (_msg.Type == Tools.Common.Enums.WechatMessageType.File)
                                                                    {
                                                                        if (attachCount > _maxCount)
                                                                        {
                                                                            context.WechatM.SendAtText($"附件数量已经达到{_maxCount}，无法继续添加。\n" +
                                                                                                        "请发送\"确定\"立即发出以上广播内容。\n" +
                                                                                                        AttackDesc(textLength, attachCount) +
                                                                                                        "若想终止以上广播，请发送\"取消\"。\n" +
                                                                                                        "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                                                                        new List<string> { _msg.WXID },
                                                                                                        _msg.Self,
                                                                                                        _msg.Sender);
                                                                            Loops(context, _msg, texts, images, files);
                                                                        }
                                                                        else
                                                                        {
                                                                            var path = context.WechatM.GetFilePath(_msg);
                                                                            if (!System.IO.File.Exists(path))
                                                                            {
                                                                                context.WechatM.SendAtText($"附件接收失败，请重新发送。\n" +
                                                                                                        "请发送\"确定\"立即发出以上广播内容。\n" +
                                                                                                        AttackDesc(textLength, attachCount) +
                                                                                                        "若想终止以上广播，请发送\"取消\"。\n" +
                                                                                                        "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                                                                        new List<string> { _msg.WXID },
                                                                                                        _msg.Self,
                                                                                                        _msg.Sender);
                                                                                Loops(context, _msg, texts, images, files);
                                                                            }
                                                                            else if (files.Contains(path))
                                                                            {
                                                                                context.WechatM.SendAtText($"你发送了重复的附件，请重新发送。\n" +
                                                                                                        "请发送\"确定\"立即发出以上广播内容。\n" +
                                                                                                        AttackDesc(textLength, attachCount) +
                                                                                                        "若想终止以上广播，请发送\"取消\"。\n" +
                                                                                                        "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                                                                        new List<string> { _msg.WXID },
                                                                                                        _msg.Self,
                                                                                                        _msg.Sender);
                                                                                Loops(context, _msg, texts, images, files);
                                                                            }
                                                                            else
                                                                            {
                                                                                files.Add(path);
                                                                                context.WechatM.SendAtText($"成功接收附件。\n" +
                                                                                                        "请发送\"确定\"立即发出以上广播内容。\n" +
                                                                                                        AttackDesc(textLength, attachCount) +
                                                                                                        "若想终止以上广播，请发送\"取消\"。\n" +
                                                                                                        "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                                                                        new List<string> { _msg.WXID },
                                                                                                        _msg.Self,
                                                                                                        _msg.Sender);
                                                                                Loops(context, _msg, texts, images, files);
                                                                            }
                                                                        }

                                                                    }
                                                                    else if (_msg.Type == Tools.Common.Enums.WechatMessageType.Image)
                                                                    {
                                                                        var path = context.WechatM.GetImagePath(_msg);
                                                                        if (!System.IO.File.Exists(path))
                                                                        {
                                                                            context.WechatM.SendAtText($"图片接收失败，请重新发送。\n" +
                                                                                                    "请发送\"确定\"立即发出以上广播内容。\n" +
                                                                                                    AttackDesc(textLength, attachCount) +
                                                                                                    "若想终止以上广播，请发送\"取消\"。\n" +
                                                                                                    "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                                                                    new List<string> { _msg.WXID },
                                                                                                    _msg.Self,
                                                                                                    _msg.Sender);
                                                                            Loops(context, _msg, texts, images, files);
                                                                        }
                                                                        else if (images.Contains(path))
                                                                        {
                                                                            context.WechatM.SendAtText($"你发送了重复的图片，请重新发送。\n" +
                                                                                                    "请发送\"确定\"立即发出以上广播内容。\n" +
                                                                                                    AttackDesc(textLength, attachCount) +
                                                                                                    "若想终止以上广播，请发送\"取消\"。\n" +
                                                                                                    "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                                                                    new List<string> { _msg.WXID },
                                                                                                    _msg.Self,
                                                                                                    _msg.Sender);
                                                                            Loops(context, _msg, texts, images, files);
                                                                        }
                                                                        else
                                                                        {
                                                                            images.Add(path);
                                                                            context.WechatM.SendAtText($"成功接收图片。\n" +
                                                                                                    "请发送\"确定\"立即发出以上广播内容。\n" +
                                                                                                    AttackDesc(textLength, attachCount) +
                                                                                                    "若想终止以上广播，请发送\"取消\"。\n" +
                                                                                                    "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                                                                    new List<string> { _msg.WXID },
                                                                                                    _msg.Self,
                                                                                                    _msg.Sender);
                                                                            Loops(context, _msg, texts, images, files);
                                                                        }
                                                                    }
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    Context.Logger.Write(ex, _continueTag);
                                                                    context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。",
                                                                                             new List<string> { _msg.WXID },
                                                                                             _msg.Self,
                                                                                             _msg.Sender);
                                                                }
                                                            }),
                                                            null,
                                                            null,
                                                            20,
                                                            _continueTag);
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
        /// 立即发出广播
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="texts"></param>
        /// <param name="images"></param>
        /// <param name="files"></param>
        private static void SendBroadCasts(Context context, Message msg, List<string> texts, List<string> images, List<string> files)
        {
            try
            {
                var text = string.Join("\n", texts);
                foreach (var robotWxid in context.WechatM.GetWechatRobotWXIDs())
                {
                    foreach (var chatroom in context.ContactsM.GetAllGroupWXID(robotWxid, false))
                    {
                        context.WechatM.SendText(text, robotWxid, chatroom);
                        foreach (var path in images)
                        {
                            context.WechatM.SendImage(path, robotWxid, chatroom);
                        }
                        foreach (var path in files)
                        {
                            context.WechatM.SendFile(path, robotWxid, chatroom);
                        }
                    }
                }

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
