﻿using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.res.communicate;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.cmd.broadcast;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.conversation
{

    internal class cmd_set_conversation_global : ICMD
    {
        public Context _context { get; set; }
        public cmd_set_conversation_global(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "更新全局对话", "更新公共对话" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_set_conversation_global";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        private string _continueTag = "cmd_add_global_conversation_continue";
        private int _maxCount = 3;
        private int _maxLength = 300;
        private ConcurrentDictionary<string, BroadCast> _cache = new();
        private string AttackDesc(int textLength, int attachCount)
        {
            var descs = new List<string>();
            if (textLength < _maxLength) descs.Add($"文本({textLength}/{_maxLength})");
            if (attachCount < _maxCount) descs.Add($"图片文件({attachCount}/{_maxCount})");
            if (descs.Count > 0) return $"你可以继续添加 {string.Join("和", descs)}，请在此发送。\n";
            else return "";
        }

        private void RemoveCache(Message msg)
        {
            var _id = BroadCast.GetID(msg);
            _cache.TryRemove(_id, out _);
        }


        async public Task Do(Message msg)
        {
            try
            {
                _context.CommunicateM.UnregistWaitMessageRequest(msg.Self, msg.Sender, msg.WXID, _continueTag);
                // 更新对话 [KEY]
                var arr = msg.ExplodeContent;
                var key = "";
                if (arr.Length < 2) return;
                key = arr[1];

                if (string.IsNullOrEmpty(key))
                {
                    var tip = new List<string>();
                    if (string.IsNullOrEmpty(key)) tip.Add("空的关键字");
                    _context.WechatM.SendAtText($"在设置对话内容时，您输入了空的关键字，设置失败。",
                                                 new List<string> { msg.WXID },
                                                 msg.Self,
                                                 msg.Sender);
                    return;
                }
                var conversation = _context.ConversationM.CheckConversationKey("", key);
                if (conversation > 0) _context.WechatM.SendAtText($"关键词[{key}]已存在于该俱乐部的对话中，继续添加对话会覆盖原有内容。\n" +
                                                                   $"可接受的回复内容包括：文本(300字以内)，图片(格式为.png/.jpg等)，文档(格式为.docx/.xlsx/.pptx/.pdf等)，图片及文档共最多3个\n" +
                                                                   $"若回复内容满足以上要求，将被直接保存\n" +
                                                                   $"请在20秒之内发出，或回复\"取消\"",
                                                                   new List<string> { msg.WXID },
                                                                   msg.Self,
                                                                   msg.Sender);
                else _context.WechatM.SendAtText($"现在请你为关键字[{key}]指定要回复的内容。\n" +
                                                 $"可接受的回复内容包括：文本(300字以内)，图片(格式为.png/.jpg等)，文档(格式为.docx/.xlsx/.pptx/.pdf等)，图片及文档共最多3个\n" +
                                                 $"若回复内容满足以上要求，将被直接保存\n" +
                                                 $"请在20秒之内发出，或回复\"取消\"",
                                                 new List<string> { msg.WXID },
                                                 msg.Self,
                                                 msg.Sender);
                var bc = new BroadCast(msg)
                {
                    Key = key,
                };

                if (_cache.ContainsKey(bc.ID)) _cache[bc.ID] = bc;
                else _cache.TryAdd(bc.ID, bc);

                Loops(msg);
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
            }
        }

        private async Task OnMessageArrival(Message msg)
        {
            try
            {
                var bcID = BroadCast.GetID(msg);
                var bc = _cache.ContainsKey(bcID) ? _cache[bcID] : null;
                if (bc is null) return;

                if (msg.Type == Tools.Common.Enums.WechatMessageType.Text)
                {
                    if (msg.Content == "取消")
                    {
                        RemoveCache(msg);
                    }
                    else if (msg.Content == "确定")
                    {
                        SaveConversation(msg, bc.Key, bc.Content, bc.Images, bc.Files);
                        return;
                    }
                    else
                    {
                        var text = msg.Content.Trim();
                        if (bc.ContentLength + text.Length >= 300)
                        {
                            _context.WechatM.SendAtText("因文本长度超长，内容增加失败。\n" +
                                                        "请发送\"确定\"完成并保存对话。\n" +
                                                        AttackDesc(bc.ContentLength, bc.AttachCount) +
                                                        "请发送\"取消\"终止操作。\n" +
                                                        "以上操作20秒内有效。",
                                                        new List<string> { msg.WXID },
                                                        msg.Self,
                                                        msg.Sender);
                            Loops(msg);
                        }
                        else
                        {
                            bc.Content.Add(text);
                            _context.WechatM.SendAtText("成功接收文本。\n" +
                                                        "请发送\"确定\"完成并保存对话。\n" +
                                                        AttackDesc(bc.ContentLength, bc.AttachCount) +
                                                        "请发送\"取消\"终止操作。\n" +
                                                        "以上操作20秒内有效。",
                                                        new List<string> { msg.WXID },
                                                        msg.Self,
                                                        msg.Sender);
                            Loops(msg);
                        }
                    }
                }
                else if (msg.Type == Tools.Common.Enums.WechatMessageType.File)
                {
                    if (bc.AttachCount > _maxCount)
                    {
                        _context.WechatM.SendAtText($"附件数量已经达到{_maxCount}，无法继续添加。\n" +
                                                   "请发送\"确定\"完成并保存对话。\n" +
                                                   AttackDesc(bc.ContentLength, bc.AttachCount) +
                                                   "请发送\"取消\"终止操作。\n" +
                                                   "以上操作20秒内有效。",
                                                   new List<string> { msg.WXID },
                                                   msg.Self,
                                                   msg.Sender);
                        Loops(msg);
                    }
                    else
                    {
                        var path = _context.WechatM.GetFilePath(msg);
                        if (!System.IO.File.Exists(path))
                        {
                            _context.WechatM.SendAtText($"附件接收失败，请重新发送。\n" +
                                                        "请发送\"确定\"完成并保存对话。\n" +
                                                        AttackDesc(bc.ContentLength, bc.AttachCount) +
                                                        "请发送\"取消\"终止操作。\n" +
                                                        "以上操作20秒内有效。",
                                                        new List<string> { msg.WXID },
                                                        msg.Self,
                                                        msg.Sender); ;
                            Loops(msg);
                        }
                        else if (bc.Files.Contains(path))
                        {
                            _context.WechatM.SendAtText($"你发送了重复的附件，请重新发送。\n" +
                                                        "请发送\"确定\"完成并保存对话。\n" +
                                                        AttackDesc(bc.ContentLength, bc.AttachCount) +
                                                        "请发送\"取消\"终止操作。\n" +
                                                        "以上操作20秒内有效。",
                                                        new List<string> { msg.WXID },
                                                        msg.Self,
                                                        msg.Sender);
                            Loops(msg);
                        }
                        else
                        {
                            bc.Files.Add(path);
                            _context.WechatM.SendAtText($"成功接收附件。\n" +
                                                        "请发送\"确定\"完成并保存对话。\n" +
                                                        AttackDesc(bc.ContentLength, bc.AttachCount) +
                                                        "请发送\"取消\"终止操作。\n" +
                                                        "以上操作20秒内有效。",
                                                        new List<string> { msg.WXID },
                                                        msg.Self,
                                                        msg.Sender);
                            Loops(msg);
                        }
                    }

                }
                else if (msg.Type == Tools.Common.Enums.WechatMessageType.Image)
                {
                    var path = _context.WechatM.GetImagePath(msg);
                    if (!System.IO.File.Exists(path))
                    {
                        _context.WechatM.SendAtText($"图片接收失败，请重新发送。\n" +
                                                    "请发送\"确定\"完成并保存对话。\n" +
                                                    AttackDesc(bc.ContentLength, bc.AttachCount) +
                                                    "请发送\"取消\"终止操作。\n" +
                                                    "以上操作20秒内有效。",
                                                    new List<string> { msg.WXID },
                                                    msg.Self,
                                                    msg.Sender);
                        Loops(msg);
                    }
                    else if (bc.Images.Contains(path))
                    {
                        _context.WechatM.SendAtText($"你发送了重复的图片，请重新发送。\n" +
                                              "请发送\"确定\"完成并保存对话。\n" +
                                              AttackDesc(bc.ContentLength, bc.AttachCount) +
                                              "请发送\"取消\"终止操作。\n" +
                                              "以上操作20秒内有效。",
                                              new List<string> { msg.WXID },
                                              msg.Self,
                                              msg.Sender);
                        Loops(msg);
                    }
                    else
                    {
                        bc.Images.Add(path);
                        _context.WechatM.SendAtText($"成功接收图片。\n" +
                                                      "请发送\"确定\"完成并保存对话。\n" +
                                                      AttackDesc(bc.ContentLength, bc.AttachCount) +
                                                      "请发送\"取消\"终止操作。\n" +
                                                      "以上操作20秒内有效。",
                                                      new List<string> { msg.WXID },
                                                      msg.Self,
                                                      msg.Sender);
                        Loops(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, _continueTag);
                _context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。",
                                       new List<string> { msg.WXID },
                msg.Self,
                msg.Sender);
            }
        }
        private void Loops(Message msg)
        {

            try
            {
                _context.CommunicateM.RegistWaitMessageRequest(msg.Self, msg.Sender, msg.WXID,
                                                                 onReceivedCallback: OnMessageArrival,
                                                                 verifier: null,
                                                                 onTimeout: new Action(() =>
                                                                 {
                                                                     RemoveCache(msg);
                                                                 }),
                                                                 acceptTypes: null,
                                                                 waitSeconds: 20,
                                                                 tag: _continueTag);
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
                _context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。",
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
        private void SaveConversation(Message msg, string key, List<string> content, List<string> images, List<string> files)
        {
            try
            {
                var txt = content.Count == 0 ? "" : string.Join("\n", content);
                var result = _context.ConversationM.UpdateGroupConversation("", key, txt, images, files);
                if (result)
                {
                    var desc = $"已经更新了新的全局对话内容\n" +
                               $"关键字：{key}";
                    if (!string.IsNullOrEmpty(txt)) desc += $"\n文本：{txt.Length}字";
                    if (images.Count > 0) desc += $"\n图片：{images.Count}个";
                    if (files.Count > 0) desc += $"\n文档：{files.Count}个";
                    _context.WechatM.SendAtText(desc,
                                               new List<string> { msg.WXID },
                                               msg.Self,
                                               msg.Sender);
                }
                else _context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
                _context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。",
                                            new List<string> { msg.WXID },
                                            msg.Self,
                                            msg.Sender);
            }
        }
    }
}
