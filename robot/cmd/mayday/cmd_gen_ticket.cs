using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.cmd.broadcast;
using RS.Snail.JJJ.robot.cmd.utils;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.mayday
{

    internal class cmd_gen_ticket : ICMD
    {
        public Context _context { get; set; }
        public cmd_gen_ticket(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "生成门票" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_gen_ticket";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.MAYDAY;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        private ConcurrentDictionary<string, MaydayTicketToken> _tokens = new();

        public void Do(Message msg)
        {
            try
            {
                if (_context.MaydayM.IsBanned(msg.Sender) || _context.MaydayM.IsWoring(msg.Sender)) return;
                var cdResult = _context.MaydayM.CheckCDs(msg.Sender);
                if (!cdResult.result)
                {
                    _context.WechatM.SendAtText(cdResult.desc, new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                _context.CommunicateM.UnregistWaitMessageRequest(msg.RoomID, msg.Sender, Tag);
                var desc = $"请发送门票生成文本。\n " +
                           $"(请注意，每行开头的名称需保留，不需填写的行可以删除)\n" +
                           $"(可发送\"门票文字模板\"获取该文本模板)" +
                           $"请在3分钟内回复以上内容，最后单独回复\"确定\"进行生成,\n" +
                           "或回复\"取消\"";
                _context.WechatM.SendAtText(desc, new List<string> { msg.Sender }, msg.RoomID);
                _tokens[MaydayTicketToken.GetID(msg)] = new MaydayTicketToken(msg);

                Loop(msg);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
                _context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。",
                                            new List<string> { msg.Sender },
                                            msg.RoomID);
            }
        }

        private void Loop(Message msg, bool shorterTime = false)
        {
            try
            {
                _context.CommunicateM.RegistWaitMessageRequest(msg.RoomID, msg.Sender,
                                                                onReceivedCallback: OnMessageArrival,
                                                                verifier: Verify,
                                                                onTimeout: new Action(() =>
                                                                {
                                                                    ClearMission(msg);
                                                                }),
                                                                acceptTypes: new List<WechatMessageType> { WechatMessageType.Text },
                                                                waitSeconds: shorterTime ? 60 : 180,
                                                                tag: Tag);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
                _context.WechatM.SendAtText("因未知原因，操作失败了，具体原因见日志。",
                                            new List<string> { msg.Sender },
                                            msg.RoomID);
            }

        }

        private bool Verify(Message msg)
        {
            switch (msg.Content)
            {
                case "门票文字模板":
                case "门票文字模版":
                case "门票列表":
                    return false;
                default:
                    return true;
            }
        }
        private void OnMessageArrival(Message msg)
        {
            try
            {
                var tokenId = MaydayTicketToken.GetID(msg);
                var token = _tokens.ContainsKey(tokenId) ? _tokens[tokenId] : null;
                if (token is null) return;

                if (msg.Content == "取消")
                {
                    ClearMission(msg);
                    _context.WechatM.SendAtText($"你已经取消了本次生成，但也浪费了今天的一次机会哦。\n(今天还可生成{_context.MaydayM.CountLeftToday(msg.Sender)}次)", new List<string> { msg.Sender }, msg.RoomID);
                }
                else if (msg.Content == "确定")
                {
                    var result = _context.MaydayM.CheckTicketToken(token);
                    if (!result.ret)
                    {
                        _context.WechatM.SendAtText($"{result.desc}\n请继续补齐以上信息，再回复\"确定\"。", new List<string> { msg.Sender }, msg.RoomID);
                        Loop(msg, true);
                    }
                    else
                    {
                        SendTicket(msg, token);
                        ClearMission(msg);
                    }
                }
                else
                {
                    var result = _context.MaydayM.ImportTicketTokenFromText(msg.Content, ref token);
                    if (!result.anyMarks)
                    {
                        Loop(msg, true);
                    }
                    else
                    {
                        if (result.result) _context.WechatM.SendAtText("请回复\"确定\"生成门票，或继续修改以上信息。", new List<string> { msg.Sender }, msg.RoomID);
                        else _context.WechatM.SendAtText($"{result.conclusion}\n请继续补齐以上信息，再回复\"确定\"。", new List<string> { msg.Sender }, msg.RoomID);
                        Loop(msg);
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

        private void ClearMission(Message msg)
        {
            try
            {
                var tokenId = MaydayTicketToken.GetID(msg);
                _tokens.TryRemove(tokenId, out _);
                _context.MaydayM.RemoveWorking(msg.Sender);
                _context.CommunicateM.UnregistWaitMessageRequest(msg.RoomID, msg.Sender, Tag, invokeTimeoutCb: false);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }

        private void SendTicket(Message msg, MaydayTicketToken token)
        {
            try
            {
                var files = _context.MaydayM.DrawTicket(token, msg);
                if (files.Count <= 0)
                {
                    _context.WechatM.SendAtText("因未知原因，门票生成失败了。",
                                                new List<string> { msg.Sender },
                                                msg.RoomID);
                }
                else
                {
                    foreach (var file in files)
                    {
                        if (file.EndsWith(".png")) _context.WechatM.SendImage(file, msg.RoomID);
                        else _context.WechatM.SendFile(file, msg.RoomID);
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

            ClearMission(msg);
        }
    }
}
