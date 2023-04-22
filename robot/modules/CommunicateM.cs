using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.res.communicate;
using RS.Snail.JJJ.clone;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.modules
{
    internal class CommunicateM : IModule
    {
        #region FIELDS
        public string ModuleName { get => "CommunicateM"; }
        public Context _context { get; set; }

        private bool _inited = false;
        public bool Inited { get => _inited; }

        private RS.Snail.JJJ.Wechat.Service _service;

        private Dictionary<string, CommandInfo> _cmds;
        private ConcurrentDictionary<string, List<WaitMessageRequest>> _waitRequests;
        private ConcurrentDictionary<string, ConcurrentQueue<Message>> _messageQueue;
        private ConcurrentDictionary<string, bool> _isTreating;
        #endregion

        #region INIT
        public CommunicateM(boot.Context context)
        {
            this._context = context;
        }
        public void Init(bool load = false)
        {
            if (_inited) return;
            _inited = true;
            LoadCMDs();
            _waitRequests = new();
            _messageQueue = new();
            _isTreating = new();
        }

        private void LoadCMDs()
        {
            _cmds = Context.Reflactor.MappingMethodsForCMD();
        }
        #endregion

        #region METHODS
        public void ReceiveMessage(dynamic data)
        {
            try
            {
                var message = new Message(data);
                var robotWXID = message.Self;
                if (!_messageQueue.ContainsKey(robotWXID)) _messageQueue.TryAdd(robotWXID, new ConcurrentQueue<Message>());
                _messageQueue[robotWXID].Enqueue(message);
                if (!GetTreatingStat(robotWXID)) Task.Run(() => TreatMessages(robotWXID));
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, "CommunicateM.ReceiveMessage");
            }

        }
        /// <summary>
        /// 取消息队列处理状态
        /// </summary>
        /// <param name="robotWXID"></param>
        /// <returns></returns>
        private bool GetTreatingStat(string robotWXID)
        {
            if (!_isTreating.ContainsKey(robotWXID)) _isTreating.TryAdd(robotWXID, false);
            return _isTreating[robotWXID];
        }
        /// <summary>
        /// 设置消息队列处理状态
        /// </summary>
        /// <param name="robotWXID"></param>
        /// <param name="stat"></param>
        public void SetTreatingStat(string robotWXID, bool stat)
        {
            if (!_isTreating.ContainsKey(robotWXID)) _isTreating.TryAdd(robotWXID, stat);
            else _isTreating[robotWXID] = stat;
        }
        #endregion

        #region TREAT
        /// <summary>
        /// 处理消息队列
        /// </summary>
        /// <param name="robotWXID"></param>
        private void TreatMessages(string robotWXID)
        {
            SetTreatingStat(robotWXID, true);
            do
            {
                if (!_messageQueue.ContainsKey(robotWXID)) break;
                if (_messageQueue[robotWXID].Count == 0) break;
                _messageQueue[robotWXID].TryDequeue(out var message);
                if (message is null) break;

                // 先检查等待响应
                if (TreatWatingRequest(message)) continue;

                // 检查ConfigM.SwitchCommunicateClose
                if (_context.ConfigsM.SwitchCommunicateClose)
                {
                    if (_context.ContactsM.QueryRole(message.Self, message.WXID, message.Sender) < include.UserRole.ADMINISTRATOR)
                    {
                        continue;
                    }
                }

                // 目前只处理文本消息
                if (message.Type == Tools.Common.Enums.WechatMessageType.Text) TreatTextMessage(message);

            } while (true);
            SetTreatingStat(robotWXID, false);
        }
        /// <summary>
        /// 处理等待响应的消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool TreatWatingRequest(Message message)
        {
            if (!_waitRequests.ContainsKey(message.Self) || _waitRequests[message.Self].Count <= 0) return false;
            var found = false;
            var now = TimeHelper.ToTimeStamp();
            foreach (var item in _waitRequests[message.Self].ToArray())
            {
                // 超时的命令不予处理
                if (item.StartTime + item.WaitTime <= now)
                {
                    item.IsFinished = true;
                    continue;
                }

                // 发送者（群/个人）吻合 &
                // 接受消息类型吻合 &
                // 验证吻合
                if (item.Sender == message.Sender && item.WXID == message.WXID &&
                    (item.AcceptTypes is null || item.AcceptTypes.Contains(message.Type)) &&
                    (item.Verify is null || item.Verify.Invoke(message)))
                {
                    item.OnReceivedCallback(message);
                    item.IsFinished = true;
                    found = true;
                }
            }

            _waitRequests[message.Self] = _waitRequests[message.Self].Where((a) => !a.IsFinished).ToList();

            return found;
        }
        /// <summary>
        /// 处理文本消息
        /// </summary>
        /// <param name="message"></param>
        private void TreatTextMessage(Message message)
        {
            // CD
            if (_context.CdM.IsInCD(message)) return;

            // 命令来自消息头部，以空格隔开
            var instru = message.Content.Split(" ").First().ToLower();
            var found = false;
            foreach (var item in _cmds)
            {
                if (item.Value.Instrus.Contains("/gc"))
                {
                    var a = 0;
                }
                if ((item.Value.AcceptType is null || item.Value.AcceptType == message.Type) &&
                    CheckEnableScene(item.Value.EnableScene, message) &&
                    ((item.Value.Instrus is not null && item.Value.Instrus.Contains(instru)) ||
                    (item.Value.InstrusJianpin is not null && item.Value.InstrusJianpin.Contains(instru))) &&
                    CheckRequestRole(item.Value.MinRole, message))
                {
                    Task.Run(() => Context.Reflactor.Invoke(item.Value, _context, message));
                    found = true;
                    break;
                }
            }

            // 特殊指令
            if (!found && message.Scene != include.ChatScene.Private)
            {
                if (message.IsCallingJijiji)
                {
                    Task.Run(() => CommonConversation(message, message.IsCallingJijiji));
                }
            }
        }
        /// <summary>
        /// 自动对话
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private bool CommonConversation(Message msg, bool isForce = false)
        {
            var response = "";
            if (msg.Scene == include.ChatScene.Private)
            {
                response = _context.ConversationM.QueryConversation("", msg.WXID, msg.Content, isForce);
            }
            else
            {
                var group = _context.ContactsM.FindGroup(msg.Self, msg.Sender);
                if (group is not null)
                {
                    var rid = group.RID ?? "";
                    response = _context.ConversationM.QueryConversation(rid, msg.WXID, msg.Content, isForce);
                }
            }

            if (string.IsNullOrEmpty(response)) return false;
            _context.WechatM.SendAtText(response, new List<string> { msg.WXID }, msg.Self, msg.Sender);

            return true;
        }

        private bool CheckEnableScene(robot.include.ChatScene? request, Message message)
        {
            if (request is null || request == include.ChatScene.All) return true;
            else return request == message.Scene;
        }

        private bool CheckRequestRole(robot.include.UserRole? request, Message message)
        {
            var role = _context.ContactsM.QueryRole(message.Self, message.WXID, message.Sender.Contains("@chatroom") ? message.Sender : "");
            return role >= request;
        }
        #endregion

        #region REGISTER
        public void RegistWaitMessageRequest(string robotWXID,
                                             string sender, string wxid,
                                             Action<Message> onReceivedCallback,
                                             Func<Message, bool>? verifier,
                                             List<RS.Tools.Common.Enums.WechatMessageType>? acceptTypes = null,
                                             int waitSeconds = 10,
                                             string tag = "")
        {
            if (!_waitRequests.ContainsKey(robotWXID)) _waitRequests.TryAdd(robotWXID, new List<WaitMessageRequest>());
            _waitRequests[robotWXID].Add(new WaitMessageRequest()
            {
                Tag = tag,
                Sender = sender,
                WXID = wxid,
                OnReceivedCallback = onReceivedCallback,
                AcceptTypes = acceptTypes,
                Verify = verifier,
                StartTime = TimeHelper.ToTimeStamp(),
                WaitTime = waitSeconds,
            });
        }

        public void UnregistWaitMessageRequest(string robotWXID, string sender, string wxid, string tag)
        {
            if (_waitRequests.ContainsKey(robotWXID))
            {
                _waitRequests[robotWXID] = _waitRequests[robotWXID].Where((a) =>
                {
                    return !(a.Tag == tag && a.Sender == sender && a.WXID == wxid);
                }).ToList();
            }
        }
        private class WaitMessageRequest
        {
            public string Tag { get; set; }
            public string Sender { get; set; }
            public string WXID { get; set; }
            public Action<Message> OnReceivedCallback { get; set; }
            public List<RS.Tools.Common.Enums.WechatMessageType>? AcceptTypes { get; set; }
            public Func<Message, bool>? Verify { get; set; }
            public long StartTime { get; set; }
            public long WaitTime { get; set; }
            public bool IsFinished { get; set; }

        }
        #endregion

    }
}
