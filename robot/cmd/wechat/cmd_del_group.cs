using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.wechat
{
    [attribute.CmdClass]
    internal class cmd_del_group
    {
        public const string Instrus = "解绑群";
        public const string Tag = "cmd_del_group";
        public const include.ChatScene EnableScene = include.ChatScene.Group;
        public const include.UserRole MinRole = include.UserRole.GROUP_HOLDER;
        public const RS.Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        private static string _DoubleCheckTag = "cmd_del_group_double_check";

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                context.CommunicateM.UnregistWaitMessageRequest(msg.Self, msg.Sender, msg.WXID, _DoubleCheckTag);

                var arr = msg.ExplodeContent;

                if (arr.Length < 2) return;

                // 解析rid
                var rid = arr.Last();
                if (!StringHelper.IsRID(rid)) return;

                // 找到俱乐部
                var club = context.ClubsM.FindClub(msg.Self, rid);

                if (club is null)
                {
                    context.WechatM.SendAtText($"要设置的俱乐部[{rid}]不存在。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                var purchase = context.PurchaseM.CheckPurchase(rid, msg);
                if (!purchase.result)
                {
                    if (!string.IsNullOrEmpty(purchase.desc))
                    {
                        context.WechatM.SendAtText(purchase.desc,
                                              new List<string> { msg.WXID },
                                              msg.Self,
                                              msg.Sender);
                    }
                    return;
                }

                var curHolder = context.ContactsM.QueryClubHolder(msg.Self, rid);
                if (string.IsNullOrEmpty(curHolder) || curHolder != msg.WXID)
                {
                    context.WechatM.SendAtText($"你目前不是俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]的会长。\n" +
                                               $"此命令必须由当前俱乐部会长本人操作。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                var group = context.ContactsM.FindGroup(msg.Self, msg.Sender);
                if (group is null)
                {
                    context.WechatM.SendAtText($"唧唧叽缺少当前微信群的资料，请联系会长使用命令\"刷新群信息\"。",
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
                        var curClub = context.ClubsM.FindClub(msg.Self, curRID);
                        context.WechatM.SendAtText($"当前微信群已经与其他俱乐部[{curClub?.Name ?? "新俱乐部"}-{curRID}]绑定。\n" +
                                                   $"此命令必须由该俱乐部会长本人操作。\n",
                                                     new List<string> { msg.WXID },
                                                     msg.Self,
                                                     msg.Sender);
                        return;
                    }
                }
                else
                {
                    context.WechatM.SendAtText($"当前微信群尚未与任何俱乐部绑定。\n",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                context.WechatM.SendAtText($"⚠️解绑群意味着当前微信群与俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]之间解除绑定。\n" +
                                           $"⚠️解绑群后，俱乐部将无法进行登录，并且当前微信群内所有以下信息将被删除：\n" +
                                           $"成员与游戏角色之间的绑定；\n" +
                                           $"所有经理的权限。\n" +
                                           $"若要继续操作，请在20秒内回复\"确定\"或\"取消\"！",
                                            new List<string> { msg.WXID },
                                            msg.Self,
                                            msg.Sender);
                context.CommunicateM.RegistWaitMessageRequest(msg.Self, msg.Sender, msg.WXID,
                                                            new Action<Message>((_msg) =>
                                                            {
                                                                if (_msg.Content != "确定") return;
                                                                var result = false;
                                                                try
                                                                {
                                                                    result = context.ContactsM.UnbindGroup(_msg.Self, _msg.Sender);
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    Context.Logger.Write(ex, _DoubleCheckTag);
                                                                }
                                                                if (result) context.WechatM.SendAtText($"俱乐部解绑完成。\n" +
                                                                                                        $"⚠️当前微信群已经成功与俱乐部俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]解除绑定。\n" +
                                                                                                        $"⚠️当前微信群中的所有绑定信息和经理权限已被清空。\n" +
                                                                                                        $"俱乐部信息和会长权限被保留，你可以重新绑定群。",
                                                                                                        new List<string> { msg.WXID },
                                                                                                        msg.Self,
                                                                                                        msg.Sender);
                                                                else context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
                                                                                                new List<string> { msg.WXID },
                                                                                                msg.Self,
                                                                                                msg.Sender);
                                                            }),
                                                            new Func<Message, bool>((_msg) =>
                                                            {
                                                                return _msg.Content == "确定" || _msg.Content == "取消";
                                                            }),
                                                            null,
                                                            20,
                                                            _DoubleCheckTag);
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
            }
        }
    }
}
