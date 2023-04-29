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

namespace RS.Snail.JJJ.robot.cmd.wechat
{

    internal class cmd_del_group : ICMD
    {
        public Context _context { get; set; }
        public cmd_del_group(Context context)
        {
            _context = context;
        }
        public List<string> Commands { get; } = new List<string> { "解绑群" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag { get; } = "cmd_del_group";
        public ChatScene EnableScene { get; } = include.ChatScene.Group;
        public UserRole MinRole { get; } = include.UserRole.GROUP_HOLDER;
        public WechatMessageType AcceptMessageType { get; } = Tools.Common.Enums.WechatMessageType.Text;

        private static string _DoubleCheckTag = "cmd_del_group_double_check";

        async public Task Do(Message msg)
        {
            try
            {
                _context.CommunicateM.UnregistWaitMessageRequest(msg.Self, msg.Sender, msg.WXID, _DoubleCheckTag);

                var arr = msg.ExplodeContent;

                if (arr.Length < 2) return;

                // 解析rid
                var rid = arr.Last();
                if (!StringHelper.IsRID(rid)) return;

                // 找到俱乐部
                var club = _context.ClubsM.FindClub(msg.Self, rid);

                if (club is null)
                {
                    _context.WechatM.SendAtText($"要设置的俱乐部[{rid}]不存在。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;

                var curHolder = _context.ContactsM.QueryClubHolder(msg.Self, rid);
                if (string.IsNullOrEmpty(curHolder) || curHolder != msg.WXID)
                {
                    _context.WechatM.SendAtText($"你目前不是俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]的会长。\n" +
                                               $"此命令必须由当前俱乐部会长本人操作。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                var group = _context.ContactsM.FindGroup(msg.Self, msg.Sender);
                if (group is null)
                {
                    _context.WechatM.SendAtText($"唧唧叽缺少当前微信群的资料，请联系会长使用命令\"刷新群信息\"。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                var curRID = group.RID;
                if (!string.IsNullOrEmpty(curRID))
                {
                    if (curRID != rid)
                    {
                        // 当前群已经与其他俱乐部绑定
                        var curClub = _context.ClubsM.FindClub(msg.Self, curRID);
                        _context.WechatM.SendAtText($"当前微信群已经与其他俱乐部[{curClub?.Name ?? "新俱乐部"}-{curRID}]绑定。\n" +
                                                    $"此命令必须由该俱乐部会长本人操作。\n",
                                                     new List<string> { msg.WXID },
                                                     msg.Self,
                                                     msg.Sender);
                        return;
                    }
                }
                else
                {
                    _context.WechatM.SendAtText($"当前微信群尚未与任何俱乐部绑定。\n",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                _context.WechatM.SendAtText($"⚠️解绑群意味着当前微信群与俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]之间解除绑定。\n" +
                                            $"⚠️解绑群后，俱乐部将无法进行登录，并且当前微信群内所有以下信息将被删除：\n" +
                                            $"成员与游戏角色之间的绑定；\n" +
                                            $"所有经理的权限。\n" +
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
                                                                             result = _context.ContactsM.UnbindGroup(_msg.Self, _msg.Sender);
                                                                         }
                                                                         catch (Exception ex)
                                                                         {
                                                                             Context.Logger.Write(ex, _DoubleCheckTag);
                                                                         }
                                                                         if (result) _context.WechatM.SendAtText($"俱乐部解绑完成。\n" +
                                                                                                                 $"⚠️当前微信群已经成功与俱乐部俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]解除绑定。\n" +
                                                                                                                 $"⚠️当前微信群中的所有绑定信息和经理权限已被清空。\n" +
                                                                                                                 $"俱乐部信息和会长权限被保留，你可以重新绑定群。",
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
                                                            waitSeconds: 20,
                                                            tag: _DoubleCheckTag);
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
            }
        }
    }
}
