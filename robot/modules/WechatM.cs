using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.X509;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.game.include;
using RS.Snail.JJJ.clone;
using RS.Tools.Common.Utils;
using RS.WechatFerry.model;
using SharpCompress.Common;
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

        private RS.WechatFerry.Wechat _wechat;

        private clone.Wechat _wechatCFG;
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
            var jo = IOHelper.GetJO("BOT\\wechat_cfg.json", false) ?? new JObject();
            _wechatCFG = new clone.Wechat(jo);
        }
        private void InitWechat()
        {
            InitLogs();
            _wechat = new(OnWechatMessageReceived, _context.IsRestart);
            _wechat.Init();
            RefreshGroupMemberNames();
        }
        /// <summary>
        /// 将wcflog的旧记录重命名为[YY-MM-DD]-wcf.txt
        /// </summary>
        private void InitLogs()
        {
            try
            {
                var wcfPath = "logs\\wcf.txt";
                if (!File.Exists(wcfPath)) return;

                string date = "";
                var content = File.ReadAllText(wcfPath);
                foreach (var line in content.Split("\n"))
                {
                    if (string.IsNullOrEmpty(line) || !line.StartsWith("[")) continue;
                    date = line.Split(" ").First().Replace("[", "");
                    break;
                }

                var today = DateTime.Now;
                // 第一行是今天的，不做变动
                if (string.IsNullOrEmpty(date) || today.ToString("YYYY=MM-dd") == date || today.ToString("yyyy-M-d") == date) return;

                var targetPath = $"logs\\{date}-wcf.txt";
                if (System.IO.File.Exists(targetPath))
                {
                    // 若新文件已存在,将旧内容追加到上一个文件并清空wcf.txt
                    System.IO.File.AppendAllText(targetPath, "\n" + content);
                    System.IO.File.WriteAllText(wcfPath, "");
                }
                else
                {
                    // 写入新文件，并删除wcf
                    System.IO.File.WriteAllText(targetPath, content);
                    System.IO.File.Delete(wcfPath);
                }
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "WechatM.InitLogs", false);
            }
        }

        #endregion

        #region MESSAGE SEND
        /// <summary>
        /// 发送文本
        /// </summary>
        /// <param name="content"></param>
        /// <param name="target"></param>
        public void SendText(string content, string target)
        {
            Context.Logger.WriteInfo("WechatM", $"[TO:{_context.ContactsM.QueryContactNick(target)}] {content}");
            if (_context.IsTest) return;
            _wechat.SendText(content, target);
        }
        /// <summary>
        /// 发送at
        /// 0~7点间不at
        /// </summary>
        /// <param name="content"></param>
        /// <param name="at"></param>
        /// <param name="target"></param>
        /// <param name="appendNickAtFirst"></param>
        public void SendAtText(string content, List<string> at, string target, bool appendNickAtFirst = true)
        {
            if (!target.EndsWith("@chatroom"))
            {
                SendText(content, target);
                return;
            }
            at = at.Where((a) => !a.EndsWith("@chatroom")).ToList();
            if (appendNickAtFirst)
            {
                var nicks = new List<string>();
                foreach (var item in at)
                {
                    var user = _context.ContactsM.FindGroupMember(target, item);
                    nicks.Add($"@{(user is null ? "蜗牛" : user.NickName)} ");
                }
                content = string.Join("", nicks) + content;
            }

            if (_context.IsTest) return;

            var hour = DateTime.Now.Hour;
            if (hour >= 0 && hour < 7)
            {
                SendText(content, target);
                return;
            }
            else
            {
                _wechat.SendText(content, target, string.Join(",", at), false);
            }
            Context.Logger.WriteInfo("WechatM", $"[TO:{_context.ContactsM.QueryContactNick(target)}] {content}");
        }
        /// <summary>
        /// 向管理群发送消息
        /// </summary>
        /// <param name="content"></param>
        /// <param name="atAdmins"></param>
        public void SendMgrNotice(string content, bool atAdmins = false)
        {
            var chatroom = _wechatCFG.ManagerGroup;
            if (string.IsNullOrEmpty(chatroom)) return;

            if (atAdmins && _wechatCFG.Admins is not null && _wechatCFG.Admins.Count > 0) SendAtText(content, _wechatCFG.Admins, chatroom, true);
            else SendText(content, chatroom);
        }
        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="target"></param>
        public void SendFile(string filePath, string target)
        {
            var flag = System.IO.File.Exists(System.IO.Path.GetFullPath(filePath));
            Context.Logger.WriteInfo("WechatM", $"[文件] [TO:{_context.ContactsM.QueryContactNick(target)}] {System.IO.Path.GetFullPath(filePath)}");
            if (_context.IsTest) return;

            _wechat.SendFile(System.IO.Path.GetFullPath(filePath), target);
        }
        /// <summary>
        /// 发送图片
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="target"></param>
        public void SendImage(string filePath, string target)
        {
            var flag = System.IO.File.Exists(System.IO.Path.GetFullPath(filePath));
            Context.Logger.WriteInfo("WechatM", $"[图片] [TO:{_context.ContactsM.QueryContactNick(target)}] {System.IO.Path.GetFullPath(filePath)}");
            if (_context.IsTest) return;

            _wechat.SendImg(System.IO.Path.GetFullPath(filePath), target);
        }
        /// <summary>
        /// 发送文章
        /// </summary>
        /// <param name="imagefilePath"></param>
        /// <param name="url"></param>
        /// <param name="title"></param>
        /// <param name="abstract"></param>
        /// <param name="target"></param>
        public void SendArtical(string name, string account, string imagefilePath, string url, string title, string @abstract, string target)
        {
            Context.Logger.WriteInfo("WechatM", $"[文章] [TO:{_context.ContactsM.QueryContactNick(target)}] [名称:{name}] [公号:{account}] [图片:{imagefilePath}] [连接:{url}] [标题:{title}] [摘要:{@abstract}]");
            if (_context.IsTest) return;

            _wechat.SendArtical(name, account, title, @abstract, url, imagefilePath, target);
        }
        /// <summary>
        /// 发送拍一拍
        /// </summary>
        /// <param name="chatroom"></param>
        /// <param name="wxid"></param>
        public void SendPat(string chatroom, string wxid)
        {
            Context.Logger.WriteInfo("WechatM", $"[拍一拍] [TO:{_context.ContactsM.QueryContactNick(chatroom)}@{_context.ContactsM.QueryGroupMemberNick(wxid, chatroom)}]");
            if (_context.IsTest) return;

            _wechat.SendPat(chatroom, wxid);
        }
        /// <summary>
        /// 转发消息
        /// </summary>
        /// <param name="msgID"></param>
        /// <param name="target"></param>
        public void ForwardMsg(ulong msgID, string target)
        {
            Context.Logger.WriteInfo("WechatM", $"[TO:{_context.ContactsM.QueryContactNick(target)}] Forward:{msgID}");
            if (_context.IsTest) return;
            _wechat.ForwardMsg(msgID, target);
        }
        #endregion

        #region MESSAGES RECEIVE
        private void OnWechatMessageReceived(RecvMsg message)
        {
            _context.CommunicateM.ReceiveMessage(message);
        }

        private void OnWechatStatChanged(string wxid, bool stat)
        {

        }
        public void StartReceive() => _wechat.StartRecv();
        public void StopReceive() => _wechat.StopRecv();
        public void StopWCF() => _wechat.DestroyWCF();
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// 全员player
        /// </summary>
        /// <returns></returns>
        public List<string> VisitorGroups() => _wechatCFG.VisitorGroups;
        /// <summary>
        /// 清除消息队列
        /// </summary>
        /// <returns></returns>
        public void ClearMessageQueue() => _wechat.ClearMsgQ();
        // public List<string> GetWechatRobotWXIDs() => _wechatCFG.Keys.ToList();
        // public dynamic Query(string wxid, string path = null) => JSONHelper.QueryCommon(_wechatCFG, wxid, path);
        public clone.Wechat? WechatCFG() => _wechatCFG;
        public string SelfWXID() => _wechat.SelfWxid();
        /// <summary>
        /// 获得接收的文件路径
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public string? GetFilePath(Message msg) => (msg.Type != Tools.Common.Enums.WechatMessageType.File || string.IsNullOrEmpty(msg.Extra)) ? "" :
            _wechat.DownloadFile(msg.MsgID, msg.Extra);

        /// <summary>
        /// 将图片转码并获得文件路径
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public string? GetImagePath(Message msg) => (msg.Type != Tools.Common.Enums.WechatMessageType.Image || string.IsNullOrEmpty(msg.Extra)) ? "" :
            _wechat.DownloadImage(msg.MsgID, msg.Extra, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WECHAT"));
        #endregion

        #region INFOS
        /// <summary>
        /// 刷新群成员昵称
        /// </summary>
        /// <param name="chatroom"></param>
        /// <returns></returns>
        public bool RefreshGroupMemberNames(string chatroom = "", bool cacheData = false)
        {
            var flag = true;
            try
            {
                if (string.IsNullOrEmpty(chatroom))
                {
                    // 机器人信息
                    var myInfo = _wechat.SelfInfo();
                    flag &= _context.ContactsM.RefreshSelfInfo(myInfo);

                    // 通讯录信息
                    var contacts = _wechat.GetContacts();
                    flag &= _context.ContactsM.RefreshContactList(contacts);
                }

                // 群成员昵称
                var groupData = _wechat.GetChatroomMemberNames(chatroom);
                flag &= _context.ContactsM.RefreshGroupMemberNicks(groupData, cacheData);

                return flag;
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, $"{ModuleName}.RefreshGroupMemberNames");
                return false;
            }
        }
        #endregion

    }
}
