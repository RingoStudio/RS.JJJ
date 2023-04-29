using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.game.include;
using RS.Snail.JJJ.clone;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.modules
{
    internal class WechatM : IModule
    {
        #region FIELDS
        public string ModuleName { get => "WechatM"; }
        public Context _context { get; set; }

        private bool _inited = false;
        public bool Inited { get => _inited; }

        private RS.Snail.JJJ.Wechat.Service _service;

        private Dictionary<string, clone.Wechat> _wechatCFG;
        #endregion

        #region INIT
        public WechatM(boot.Context context)
        {
            this._context = context;
        }
        public void Init(bool load = false)
        {
            if (_inited) return;
            _inited = true;
            LoadCSV();
            InitWechat();
        }

        private void LoadCSV()
        {
            var jo = IOHelper.GetJO("BOT\\wechat_cfg.json", false) ?? new JArray();
            _wechatCFG = new();
            foreach (var item in jo)
            {
                var wxid = JSONHelper.ParseString(item["wxid"]);
                _wechatCFG.TryAdd(wxid, new clone.Wechat(item));
            }
        }
        private void InitWechat()
        {
            _service = new Wechat.Service(_context.IsRestart, _context.IsTest);
            _service.Init(GetWechatRobotWXIDs(), msgCallback: OnWechatMessageReceived, wechatStatCallback: OnWechatStatChanged);
        }

        #endregion

        #region MESSAGE SEND
        /// <summary>
        /// 发送文本
        /// </summary>
        /// <param name="content"></param>
        /// <param name="robotWxid"></param>
        /// <param name="target"></param>
        public void SendText(string content, string robotWxid, string target) => _service.MsgSendText(robotWxid, target, content);
        /// <summary>
        /// 发送at
        /// 0~7点间不at
        /// </summary>
        /// <param name="content"></param>
        /// <param name="at"></param>
        /// <param name="robotWxid"></param>
        /// <param name="target"></param>
        /// <param name="appendNickAtFirst"></param>
        public void SendAtText(string content, List<string> at, string robotWxid, string target, bool appendNickAtFirst = true)
        {
            if (!target.EndsWith("@chatroom"))
            {
                SendText(content, robotWxid, target);
                return;
            }
            at = at.Where((a) => !a.EndsWith("@chatroom")).ToList();
            if (appendNickAtFirst)
            {
                var nicks = new List<string>();
                foreach (var item in at)
                {
                    var user = _context.ContactsM.FindGroupMember(robotWxid, target, item);
                    nicks.Add($"@{(user is null ? "蜗牛" : user.NickName)}{(char)0x85}");
                }
                content = string.Join("", nicks) + content;
            }
            var hour = DateTime.Now.Hour;
            if (hour >= 0 && hour < 7) SendText(content, robotWxid, target);
            else _service.MsgSendAt(robotWxid, target, at, content, false);
        }
        /// <summary>
        /// 向管理群发送消息
        /// </summary>
        /// <param name="content"></param>
        /// <param name="robotWxid"></param>
        /// <param name="atAdmins"></param>

        public void SendMgrNotice(string content, string robotWxid = "", bool atAdmins = false)
        {
            foreach (var cfg in _wechatCFG)
            {
                if (!string.IsNullOrEmpty(robotWxid) && robotWxid != cfg.Key) continue;
                var chatroom = cfg.Value.ManagerGroup;
                if (string.IsNullOrEmpty(chatroom)) continue;

                if (atAdmins && cfg.Value.Admins is not null && cfg.Value.Admins.Count > 0) SendAtText(content, cfg.Value.Admins, cfg.Key, chatroom, true);
                else SendText(content, cfg.Key, chatroom);
            }
        }
        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="robotWxid"></param>
        /// <param name="target"></param>
        public void SendFile(string filePath, string robotWxid, string target) => _service.MsgSendFile(robotWxid, target, filePath);
        /// <summary>
        /// 发送图片
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="robotWxid"></param>
        /// <param name="target"></param>
        public void SendImage(string filePath, string robotWxid, string target) => _service.MsgSendImage(robotWxid, target, filePath);
        /// <summary>
        /// 发送文章
        /// </summary>
        /// <param name="imagefilePath"></param>
        /// <param name="url"></param>
        /// <param name="title"></param>
        /// <param name="abstract"></param>
        /// <param name="robotWxid"></param>
        /// <param name="target"></param>
        public void SendArtical(string imagefilePath, string url, string title, string @abstract, string robotWxid, string target) => _service.MsgSendArtical(robotWxid, target, title, @abstract, url, imagefilePath);
        #endregion

        #region MESSAGES RECEIVE
        private async Task OnWechatMessageReceived(dynamic message)
        {
            await _context.CommunicateM.ReceiveMessage(message);
        }

        private async Task OnWechatStatChanged(string wxid, bool stat)
        {

        }
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// 清除消息队列
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <returns></returns>
        public bool ClearMessageQueue(string robotWxid = "") => _service.ClearMessageQueue(robotWxid);
        public List<string> GetWechatRobotWXIDs() => _wechatCFG.Keys.ToList();
        // public dynamic Query(string wxid, string path = null) => JSONHelper.QueryCommon(_wechatCFG, wxid, path);
        public clone.Wechat? FindWechatCFG(string wxid)
        {
            return _wechatCFG.ContainsKey(wxid) ? _wechatCFG[wxid] : null;
        }
        /// <summary>
        /// 获得接收的文件路径
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public string GetFilePath(Message msg)
        {
            return System.IO.Path.Combine(include.files.WechatFileRoot, msg.FilePath);
        }
        /// <summary>
        /// 将图片转码并获得文件路径
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public string? GetImagePath(Message msg)
        {
            var path = System.IO.Path.Combine(include.files.WechatFileRoot, msg.FilePath);
            return RS.Tools.Common.Utils.WechatFileHelper.DecodeWechatImageDat(path, "WECHAT\\");
        }
        #endregion

        #region INFOS
        public bool RefreshGroupMemberNames(string robotWxid, string chatroom = "")
        {
            var flag = true;
            try
            {
                if (string.IsNullOrEmpty(chatroom))
                {
                    // 机器人信息
                    var myInfo = _service.GetSelfInfo(robotWxid);
                    flag &= _context.ContactsM.RefreshSelfInfo(robotWxid, myInfo);

                    // 通讯录信息
                    var contacts = _service.ContactGetList(robotWxid);
                    flag &= _context.ContactsM.RefreshContactList(robotWxid, contacts);
                }

                // 群成员昵称
                var groupData = _service.ContaceChatGroupMemberNames(robotWxid, chatroom);
                flag &= _context.ContactsM.RefreshGroupMemberNicks(robotWxid, groupData);

                return flag;
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, $"{ModuleName}.RefreshGroupMemberNames");
                return false;
            }
        }
        #endregion

    }
}
