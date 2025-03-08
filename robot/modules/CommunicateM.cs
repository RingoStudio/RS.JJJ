using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.res.communicate;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.cmd;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Utils;
using RS.WechatFerry.model;
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

        private Dictionary<string, ICMD> _cmds;
        private ConcurrentDictionary<string, WaitMessageRequest> _waitRequests;
        private ConcurrentQueue<Message> _messageQueue;
        private bool _isTreating;
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

        /// <summary>
        /// 实例化所有实现ICMD接口的命令类型
        /// </summary>
        private void LoadCMDs()
        {
            //_cmds = Context.Reflactor.MappingMethodsForCMD();
            _cmds = new();
            var types = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(x => x.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(ICMD))))
                        .ToArray();
            foreach (var type in types)
            {
                try
                {
                    ICMD? cmd = Activator.CreateInstance(type, _context) as ICMD;
                    if (cmd is null) continue;

                    var name = cmd.Tag;

                    if (_cmds.ContainsKey(name)) continue;

                    _cmds.TryAdd(name, cmd);
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }
        #endregion

        #region METHODS
        public void ReceiveMessage(RecvMsg data)
        {
            try
            {
                //if (data.Sender != "wxid_4rcudy2bjq3422" && !data.Content.Contains("所有人")) return;
                //if (data.Sender == "ringoo")
                //{
                //    if (data.MessageType == Tools.Common.Enums.WechatMessageType.AppCard)
                //    {
                //        var msgid = data.ID;
                //        _context.WechatM.ForwardMsg(msgid, data.RoomID);
                //    }
                //    Console.WriteLine(data.GetDesc());
                //}
                var message = new Message(data, _context.WechatM.SelfWXID());
                _messageQueue.Enqueue(message);
                if (!GetTreatingStat()) TreatMessages();
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "CommunicateM.ReceiveMessage");
            }

        }
        /// <summary>
        /// 取消息队列处理状态
        /// </summary>
        /// <returns></returns>
        private bool GetTreatingStat() => _isTreating;
        /// <summary>
        /// 设置消息队列处理状态
        /// </summary>
        /// <param name="stat"></param>
        public void SetTreatingStat(bool stat) => _isTreating = stat;
        #endregion

        #region TREAT
        /// <summary>
        /// 处理消息队列
        /// </summary>
        private void TreatMessages()
        {
            SetTreatingStat(true);
            do
            {
                if (_messageQueue.Count == 0) break;
                _messageQueue.TryDequeue(out var message);

                // 先检查等待响应
                if (TreatWatingRequest(message)) continue;

                var role = _context.ContactsM.QueryRole(message.Sender, message.RoomID);

                // 检查ConfigM.SwitchCommunicateClose
                if (_context.ConfigsM.SwitchCommunicateClose)
                {
                    if (role < include.UserRole.ADMINISTRATOR)
                    {
                        continue;
                    }
                }

                // 拍一拍
                if (message.Type == Tools.Common.Enums.WechatMessageType.Recall && role >= include.UserRole.PLAYER && message.Content.Contains("拍了拍我的屏幕说信号不太好"))
                {
                    _context.WechatM.SendPat(message.RoomID, message.Sender);
                    continue;
                }

                // 目前只处理文本消息
                if (message.Type == Tools.Common.Enums.WechatMessageType.Text) TreatTextMessage(message);

            } while (true);
            SetTreatingStat(false);
        }
        /// <summary>
        /// 处理等待响应的消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool TreatWatingRequest(Message message)
        {
            var found = false;
            var now = TimeHelper.ToTimeStamp();
            var remove = new List<string>();

            foreach (var item in _waitRequests.Values)
            {
                // 超时的命令不予处理
                if (item.StartTime + item.WaitTime <= now)
                {
                    item.OnTimeOut?.Invoke();
                    item.IsFinished = true;
                    continue;
                }

                // 发送者（群/个人）吻合 &
                // 接受消息类型吻合 &
                // 验证吻合
                if (item.Sender == message.Sender && (item.RoomID ?? "") == (message.RoomID ?? "") &&
                   (item.AcceptTypes is null || item.AcceptTypes.Contains(message.Type)) &&
                   (item.Verify is null || item.Verify.Invoke(message)))
                {
                    if (item.OnReceivedCallback is not null) item.OnReceivedCallback(message);
                    item.IsFinished = true;
                    found = true;
                }

                if (item.IsFinished) remove.Add(item.ID);
            }

            foreach (var key in remove) _waitRequests.TryRemove(key, out _);

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
            var instru = (message.ExplodeContent.FirstOrDefault() ?? "").ToLower();
            foreach (var item in _cmds)
            {

                if (item.Value.AcceptMessageType != message.Type) continue;
                if (!CheckEnableScene(item.Value.EnableScene, message)) continue;
                if (!item.Value.Commands.Contains(instru) &&
                   !item.Value.CommandsJP.Contains(instru) &&
                   !item.Value.CommandsQP.Contains(instru)) continue;
                if (!CheckRequestRole(item.Value.MinRole, message)) continue;

                Task.Run(() => item.Value.Do(message));
                return;
            }

            // 特殊指令
            if (message.Scene != include.ChatScene.Private)
            {

                if (message.IsCallingJijiji && _context.ContactsM.QueryRole(message.Sender, message.RoomID) >= include.UserRole.NORMAL)
                {
                    if (message.IsAtAll)
                    {
                        if (message.Sender == "guoyi142159")
                        {
                            // 跳姐专用响应
                            _context.WechatM.SendText("好的跳姐[好的]", message.RoomID);

                        }
                        else
                        {
                            // 随机
                            if (message.MsgID % 2 == 0)
                            {
                                _context.WechatM.SendText($"我不[抠鼻]", message.RoomID);
                            }
                            else
                            {
                                _context.WechatM.SendText($"收到[好的]", message.RoomID);
                            }
                        }
                    }
                    else CommonConversation(message, message.IsCallingJijiji);
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
            if (_context.ConfigsM.SwitchConversationClose) return false;
            dynamic? response = null;
            if (msg.Scene == include.ChatScene.Private)
            {
                response = _context.ConversationM.QueryConversation("", msg.Sender, msg.Content, isForce);
            }
            else
            {
                var group = _context.ContactsM.FindGroup(msg.RoomID);
                if (group is not null)
                {
                    var rid = group.RID ?? "";
                    response = _context.ConversationM.QueryConversation(rid, msg.Sender, msg.Content, isForce);
                }
                else
                {
                    response = _context.ConversationM.QueryConversation("", msg.Sender, msg.Content, isForce);
                }
            }

            if (response is null) return false;
            return _context.ConversationM.SendResponse(response, msg);
        }

        private bool CheckEnableScene(robot.include.ChatScene? request, Message message)
        {
            if (request is null || request == include.ChatScene.All) return true;
            else return request == message.Scene;
        }

        private bool CheckRequestRole(robot.include.UserRole? request, Message message)
        {
            var role = _context.ContactsM.QueryRole(message.Sender);
            if (role >= request) return true;
            if (message.IsGroup)
            {
                if (request == include.UserRole.PLAYER) return _context.ContactsM.IsPlayerGroup(message.RoomID);
                if (request == include.UserRole.MAYDAY) return _context.ContactsM.IsMaydayGroup(message.RoomID);
            }

            return false;
        }
        #endregion

        #region REGISTER
        public void RegistWaitMessageRequest(string roomID,
                                             string sender,
                                             Action<Message> onReceivedCallback,
                                             Func<Message, bool>? verifier = null,
                                             Action onTimeout = null,
                                             List<RS.Tools.Common.Enums.WechatMessageType>? acceptTypes = null,
                                             int waitSeconds = 10,
                                             string tag = "")
        {

            var req = new WaitMessageRequest()
            {
                Tag = tag,
                Sender = sender,
                RoomID = roomID,
                OnReceivedCallback = onReceivedCallback,
                OnTimeOut = onTimeout,
                AcceptTypes = acceptTypes,
                Verify = verifier,
                StartTime = TimeHelper.ToTimeStamp(),
                WaitTime = waitSeconds,
            };

            _waitRequests.TryAdd(req.ID, req);
        }


        public void UnregistWaitMessageRequest(string roomID, string sender, string tag, bool invokeTimeoutCb = true)
        {
            var remove = new List<string>();
            foreach (var item in _waitRequests.Values)
            {
                if (invokeTimeoutCb) item.OnTimeOut?.Invoke();
                if (item.Tag == tag && item.Sender == sender && item.RoomID == roomID) remove.Add(item.ID);
            }

            foreach (var key in remove) _waitRequests.TryRemove(key, out _);
        }
        private class WaitMessageRequest
        {
            public string Tag { get; set; }
            public string Sender { get; set; }
            public string RoomID { get; set; }
            public Action<Message>? OnReceivedCallback { get; set; }
            public Action? OnTimeOut { get; set; }
            public List<RS.Tools.Common.Enums.WechatMessageType>? AcceptTypes { get; set; }
            public Func<Message, bool>? Verify { get; set; }
            public long StartTime { get; set; }
            public long WaitTime { get; set; }
            public bool IsFinished { get; set; }

            public string ID { get => $"{Tag}_{Sender}_{RoomID}_{StartTime}"; }
        }
        #endregion

    }
}
