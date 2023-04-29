using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.club
{

    internal class cmd_del_club : ICMD
    {

        public Context _context { get; set; }
        public cmd_del_club(Context context)
        {
            _context = context;
        }
        public List<string> Commands { get; } = new List<string> { "移除俱乐部" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag { get; } = "cmd_del_club";
        public ChatScene EnableScene { get; } = include.ChatScene.All;
        public UserRole MinRole { get; } = include.UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType { get; } = Tools.Common.Enums.WechatMessageType.Text;

        private static string _DoubleCheckTag = "cmd_del_club_double_check";


        async public Task Do(Message msg)
        {
            _context.CommunicateM.UnregistWaitMessageRequest(msg.Self, msg.Sender, msg.WXID, _DoubleCheckTag);
            try
            {
                // 移除俱乐部 [RID]
                var arr = msg.ExplodeContent;
                if (arr.Length < 2) return;

                var rid = arr[1];
                if (!StringHelper.IsRID(rid)) return;

                var club = _context.ClubsM.FindClub(msg.Self, rid);
                if (club is null)
                {
                    _context.WechatM.SendAtText($"⚠️要移除的俱乐部 [{rid}] 不存在！",
                                                   new List<string> { msg.WXID },
                                                   msg.Self,
                                                   msg.Sender);

                    return;
                }

                _context.WechatM.SendAtText($"⚠️移除俱乐部意味着当前微信群与俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]之间解除绑定。\n" +
                                            $"⚠️移除俱乐部后，俱乐部的登录账号等信息将被清除。" +
                                            $"⚠️移除俱乐部后，俱乐部将无法进行登录，并且当前微信群内所有以下信息将被删除：\n" +
                                            $"成员与游戏角色之间的绑定；\n" +
                                            $"会长以及所有经理的权限。\n" +
                                            $"若要继续操作，请在20秒内回复\"确定\"或\"取消\"！",
                                             new List<string> { msg.WXID },
                                             msg.Self,
                                             msg.Sender);
                _context.CommunicateM.RegistWaitMessageRequest(msg.Self, msg.Sender, msg.WXID,
                                                             onReceivedCallback: new Func<Message, Task>((_msg) =>
                                                             {
                                                                 return Task.Run(() =>
                                                                    {

                                                                        if (_msg.Content != "确定") return;
                                                                        var result = false;
                                                                        try
                                                                        {
                                                                            result = _context.ClubsM.DelClub(_msg.Self, rid);
                                                                            if (result) result = _context.ContactsM.UnbindGroup(_msg.Self, _msg.Sender);
                                                                            if (result) result = _context.ContactsM.DelHolder(_msg.Self, rid);
                                                                        }
                                                                        catch (Exception ex)
                                                                        {
                                                                            Context.Logger.Write(ex, _DoubleCheckTag);
                                                                        }
                                                                        if (result) _context.WechatM.SendAtText($"移除俱乐部完成。\n" +
                                                                                                                $"⚠️当前微信群已经成功与俱乐部俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]绑定。\n" +
                                                                                                                $"⚠️当前微信群中的所有绑定信息和经理权限已被清空。\n" +
                                                                                                                $"⚠️当前俱乐部所有信息，包括登录账号、会长权限等已被清空。\n",
                                                                                                                new List<string> { msg.WXID },
                                                                                                                msg.Self,
                                                                                                                msg.Sender);
                                                                        else _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
                                                                                                        new List<string> { msg.WXID },
                                                                                                        msg.Self,
                                                                                                        msg.Sender);
                                                                    });
                                                             }),
                                                            verifier: new Func<Message, bool>((_msg) =>
                                                             {
                                                                 return _msg.Content == "确定" || _msg.Content == "取消";
                                                             }),
                                                            acceptTypes: null,
                                                            waitSeconds: 20,
                                                            tag: _DoubleCheckTag);

            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
                _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
            }
        }
    }
}
