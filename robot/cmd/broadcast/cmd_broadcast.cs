using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Org.BouncyCastle.Utilities;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.res.communicate;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.cmd.broadcast;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace RS.Snail.JJJ.robot.cmd.misc
{
    internal class cmd_broadcast : ICMD
    {
        public Context _context { get; set; }
        public List<string> Commands { get; } = new List<string> { "广播" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag { get; } = "cmd_broadcast";
        public ChatScene EnableScene { get; } = include.ChatScene.All;
        public UserRole MinRole { get; } = include.UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType { get; } = Tools.Common.Enums.WechatMessageType.Text;

        private string _continueTag = "cmd_broadcast_continue";
        private int _maxCount = 3;
        private int _maxLength = 500;

        private ConcurrentDictionary<string, BroadCast> _broadCasts = new();

        public cmd_broadcast(Context context)
        {
            _context = context;
        }

        public void Do(Message msg)
        {
            try
            {
                _context.CommunicateM.UnregistWaitMessageRequest(msg.RoomID, msg.Sender, _continueTag);
                var arr = msg.ExplodeContent;
                if (arr.Length < 2) return;
                var instru = arr.First();
                var bc = new BroadCast(msg);
                bc.Content.Add(msg.Content.Substring(instru.Length).Trim());
                if (_broadCasts.ContainsKey(bc.ID)) _broadCasts[bc.ID] = bc;
                else _broadCasts.TryAdd(bc.ID, bc);

                _context.WechatM.SendAtText("请发送\"确定\"立即向绑定俱乐部的群发出以上广播内容。\n" +
                                           "若想要添加文本/文件/图片，请在此发送。\n" +
                                           "你最多可以发送不超过500字的文本，和最多3个附件。\n" +
                                           "若想终止以上广播，请发送\"取消\"。\n" +
                                           "以上操作20秒内有效，超时未回复将自动终止发送。",
                                           new List<string> { msg.Sender },
                                           msg.RoomID,
                                           true);
                Loops(msg);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }

        private string AttackDesc(int textLength, int attachCount)
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
        private void Loops(Message msg)
        {

            try
            {
                _context.CommunicateM.RegistWaitMessageRequest(msg.RoomID, msg.Sender,
                                                              onReceivedCallback: OnMessageArrival,
                                                              verifier: null,
                                                              onTimeout: new Action(() =>
                                                              {
                                                                  RemoveBroadCast(msg);
                                                              }),
                                                              acceptTypes: null,
                                                              waitSeconds: 20,
                                                              tag: _continueTag);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
                _context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。",
                                            new List<string> { msg.Sender },
                                            msg.RoomID);
            }
        }

        private void RemoveBroadCast(Message msg)
        {
            var _id = BroadCast.GetID(msg);
            _broadCasts.TryRemove(_id, out _);
        }

        private void OnMessageArrival(Message msg)
        {
            try
            {
                var bcID = BroadCast.GetID(msg);
                var bc = _broadCasts.ContainsKey(bcID) ? _broadCasts[bcID] : null;
                if (bc is null) return;

                if (msg.Type == Tools.Common.Enums.WechatMessageType.Text)
                {
                    if (msg.Content == "取消")
                    {
                        RemoveBroadCast(msg);
                    }
                    else if (msg.Content == "确定")
                    {
                        SendBroadCasts(msg, bc.Content, bc.Images, bc.Files);
                        return;
                    }
                    else
                    {
                        var text = msg.Content.Trim();
                        if (bc.ContentLength + text.Length >= 500)
                        {
                            _context.WechatM.SendAtText("因文本长度超长，内容增加失败。\n" +
                                                        "请发送\"确定\"立即发出以上广播内容。\n" +
                                                        AttackDesc(bc.ContentLength, bc.AttachCount) +
                                                        "若想终止以上广播，请发送\"取消\"。\n" +
                                                        "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                        new List<string> { msg.Sender },
                                                        msg.RoomID);
                            Loops(msg);
                        }
                        else
                        {
                            bc.Content.Add(text);
                            _context.WechatM.SendAtText("成功接收文本。\n" +
                                                        "请发送\"确定\"立即发出以上广播内容。\n" +
                                                        AttackDesc(bc.ContentLength, bc.AttachCount) +
                                                        "若想终止以上广播，请发送\"取消\"。\n" +
                                                        "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                        new List<string> { msg.Sender },
                                                        msg.RoomID);
                            Loops(msg);
                        }
                    }
                }
                else if (msg.Type == Tools.Common.Enums.WechatMessageType.File)
                {
                    // 文件附件
                    if (bc.AttachCount > _maxCount)
                    {
                        _context.WechatM.SendAtText($"附件数量已经达到{_maxCount}，无法继续添加。\n" +
                                                   "请发送\"确定\"立即发出以上广播内容。\n" +
                                                   AttackDesc(bc.ContentLength, bc.AttachCount) +
                                                   "若想终止以上广播，请发送\"取消\"。\n" +
                                                   "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                   new List<string> { msg.Sender },
                                                   msg.RoomID);
                        Loops(msg);
                    }
                    else
                    {
                        var path = _context.WechatM.GetFilePath(msg);
                        if (!System.IO.File.Exists(path))
                        {
                            _context.WechatM.SendAtText($"附件接收失败，请重新发送。\n" +
                                                        "请发送\"确定\"立即发出以上广播内容。\n" +
                                                        AttackDesc(bc.ContentLength, bc.AttachCount) +
                                                        "若想终止以上广播，请发送\"取消\"。\n" +
                                                        "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                        new List<string> { msg.Sender },
                                                        msg.RoomID); ;
                            Loops(msg);
                        }
                        else if (bc.Files.Contains(path))
                        {
                            _context.WechatM.SendAtText($"你发送了重复的附件，请重新发送。\n" +
                                                        "请发送\"确定\"立即发出以上广播内容。\n" +
                                                        AttackDesc(bc.ContentLength, bc.AttachCount) +
                                                        "若想终止以上广播，请发送\"取消\"。\n" +
                                                        "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                        new List<string> { msg.Sender },
                                                        msg.RoomID);
                            Loops(msg);
                        }
                        else
                        {
                            bc.Files.Add(path);
                            _context.WechatM.SendAtText($"成功接收附件。\n" +
                                                        "请发送\"确定\"立即发出以上广播内容。\n" +
                                                        AttackDesc(bc.ContentLength, bc.AttachCount) +
                                                        "若想终止以上广播，请发送\"取消\"。\n" +
                                                        "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                        new List<string> { msg.Sender },
                                                        msg.RoomID);
                            Loops(msg);
                        }
                    }

                }
                else if (msg.Type == Tools.Common.Enums.WechatMessageType.Image)
                {
                    // 图片附件
                    var path = _context.WechatM.GetImagePath(msg);
                    if (!System.IO.File.Exists(path))
                    {
                        _context.WechatM.SendAtText($"图片接收失败，请重新发送。\n" +
                                                    "请发送\"确定\"立即发出以上广播内容。\n" +
                                                    AttackDesc(bc.ContentLength, bc.AttachCount) +
                                                    "若想终止以上广播，请发送\"取消\"。\n" +
                                                    "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                    new List<string> { msg.Sender },
                                                    msg.RoomID);
                        Loops(msg);
                    }
                    else if (bc.Images.Contains(path))
                    {
                        _context.WechatM.SendAtText($"你发送了重复的图片，请重新发送。\n" +
                                              "请发送\"确定\"立即发出以上广播内容。\n" +
                                              AttackDesc(bc.ContentLength, bc.AttachCount) +
                                              "若想终止以上广播，请发送\"取消\"。\n" +
                                              "以上操作20秒内有效，超时未回复将自动终止发送。",
                                              new List<string> { msg.Sender },
                                              msg.RoomID);
                        Loops(msg);
                    }
                    else
                    {
                        bc.Images.Add(path);
                        _context.WechatM.SendAtText($"成功接收图片。\n" +
                                                      "请发送\"确定\"立即发出以上广播内容。\n" +
                                                      AttackDesc(bc.ContentLength, bc.AttachCount) +
                                                      "若想终止以上广播，请发送\"取消\"。\n" +
                                                      "以上操作20秒内有效，超时未回复将自动终止发送。",
                                                      new List<string> { msg.Sender },
                                                      msg.RoomID);
                        Loops(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, _continueTag);
                _context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。",
                                       new List<string> { msg.Sender },
                msg.RoomID);
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
        private void SendBroadCasts(Message msg, List<string> texts, List<string> images, List<string> files)
        {
            try
            {
                var text = string.Join("\n", texts);
                foreach (var chatroom in _context.ContactsM.GetAllGroupWXID(true))
                {
                    _context.WechatM.SendText(text, chatroom);
                    foreach (var path in images)
                    {
                        _context.WechatM.SendImage(path,chatroom);
                    }
                    foreach (var path in files)
                    {
                        _context.WechatM.SendFile(path, chatroom);
                    }
                }
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
                _context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。",
                                            new List<string> { msg.Sender },
                                            msg.RoomID);
            }
        }
    }
}
