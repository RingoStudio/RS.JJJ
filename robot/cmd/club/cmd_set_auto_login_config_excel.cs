using RS.Snail.JJJ.boot;
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

namespace RS.Snail.JJJ.robot.cmd.club
{

    internal class set_auto_login_config_excel : ICMD
    {
        public Context _context { get; set; }
        public set_auto_login_config_excel(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "上传自动登录设置表", "回传自动登录设置表", "上传自动登录配置表", "回传自动登录配置表" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "set_auto_login_config_excel";
        private string _continueTag = "set_auto_login_config_excel_continue";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        public void Do(Message msg)
        {
            try
            {
                _context.CommunicateM.UnregistWaitMessageRequest(msg.RoomID, msg.Sender, _continueTag);
                // 找到群
                var group = _context.ContactsM.FindGroup(msg.RoomID);
                if (group is null)
                {
                    _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
                                                new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                // 找到俱乐部
                var rid = group.RID;

                // 检查本俱乐部权限
                if (_context.ContactsM.QueryRole(msg.Sender, rid: rid) < MinRole)
                {
                    _context.WechatM.SendAtText($"您没有修改该俱乐部相关信息的权限。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                var club = _context.ClubsM.FindClub(rid);
                if (club is null)
                {
                    _context.WechatM.SendAtText($"⚠️要查询的俱乐部[{rid}]不存在。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }


                if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;

                _context.WechatM.SendAtText($"现在请你发送\"自动登录配置表.xlsx\"。\n" +
                                            $"请在3分钟之内发出，或回复\"取消\"",
                                            new List<string> { msg.Sender }, msg.RoomID);

                Loops(msg, rid);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }

        private void Loops(Message msg, string rid)
        {
            try
            {
                _context.CommunicateM.RegistWaitMessageRequest(msg.RoomID, msg.Sender,
                                                              onReceivedCallback: new Action<Message>(_msg => OnMessageArrival(_msg, rid)),
                                                              verifier: Verifier,
                                                              onTimeout: new Action(() =>
                                                              {
                                                                  //RemoveCache(msg);
                                                              }),
                                                              acceptTypes: null,
                                                              waitSeconds: 180,
                                                              tag: _continueTag);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
                _context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。", new List<string> { msg.Sender }, msg.RoomID);
            }
        }

        private void OnMessageArrival(Message msg, string rid)
        {
            try
            {
                if (msg.Type == WechatMessageType.File)
                {
                    var path = _context.WechatM.GetFilePath(msg);
                    if (!System.IO.File.Exists(path))
                    {
                        _context.WechatM.SendAtText($"附件接收失败，请重新发送。\n" +
                                                       "请发送\"取消\"终止操作。\n" +
                                                       "以上操作180秒内有效。",
                                                       new List<string> { msg.Sender }, msg.RoomID);
                        Loops(msg, rid);
                    }
                    else if (!path.ToLower().EndsWith(".xlsx"))
                    {
                        _context.WechatM.SendAtText($"你发送了错误的文件格式，请重新发送。\n" +
                                                     "请发送\"取消\"终止操作。\n" +
                                                     "以上操作180秒内有效。",
                                                     new List<string> { msg.Sender }, msg.RoomID);
                        Loops(msg, rid);
                    }
                    else
                    {
                        var result = _context.ClubsM.SetClubAutoLoginConfigSheet(path, rid);
                        if (result.result) _context.WechatM.SendAtText($"自动登录配置表接收并处理成功。\n" +
                                                                       "之后你的俱乐部自动登录过程将按照此表进行。\n" +
                                                                       $"{emoji.SHOUZHI}发送\"查看俱乐部\"可查看你的俱乐部自动登录配置。",
                                                                        new List<string> { msg.Sender }, msg.RoomID);
                        else _context.WechatM.SendAtText(result.desc ?? "因未知原因，操作失败了。", new List<string> { msg.Sender }, msg.RoomID);
                    }
                }
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, _continueTag);
                _context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。", new List<string> { msg.Sender }, msg.RoomID);
            }
        }
        private bool Verifier(Message msg)
        {
            if (msg.Type != WechatMessageType.File) return false;
            return true;
        }
    }
}
