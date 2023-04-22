using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.wechat
{
    [attribute.CmdClass]
    internal class cmd_set_group
    {
        public const string Instrus = "绑定群";
        public const string Tag = "cmd_set_group";
        public const include.ChatScene EnableScene = include.ChatScene.Group;
        public const include.UserRole MinRole = include.UserRole.GROUP_HOLDER;
        public const RS.Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        private static string _moveGroupTag = "cmd_set_group_move";

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                context.CommunicateM.UnregistWaitMessageRequest(msg.Self, msg.Sender, msg.WXID, _moveGroupTag);

                var arr = msg.ExplodeContent;

                if (arr.Length < 2) return;

                // 解析rid
                var rid = arr.Last();
                if (!StringHelper.IsRID(rid)) return;

                // 找到俱乐部
                var club = context.ClubsM.FindClub(msg.Self, rid);

                if (club is null)
                {
                    context.WechatM.SendAtText($"⚠️要设置的俱乐部[{rid}]不存在。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                var curHolder = context.ContactsM.QueryClubHolder(msg.Self, rid);
                if (string.IsNullOrEmpty(curHolder) || curHolder != msg.WXID)
                {
                    context.WechatM.SendAtText($"⚠️你目前不是俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]的会长。\n" +
                                               $"⚠️此命令必须由当前俱乐部会长本人操作。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                var group = context.ContactsM.FindGroup(msg.Self, msg.Sender);
                if (group is null)
                {
                    context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }


                var oriGroup = context.ContactsM.FindGroupByRID(msg.Self, rid);
                if (oriGroup is not null)
                {
                    if (oriGroup.WXID == group.WXID)
                    {
                        context.WechatM.SendAtText($"当前微信群已经与俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]绑定，无需重复操作。\n",
                                                    new List<string> { msg.WXID },
                                                    msg.Self,
                                                    msg.Sender);
                        return;
                    }
                    // 当前俱乐部已经绑定在其他群
                    else
                    {

                        context.WechatM.SendAtText($"当前俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]已经与其他微信群[{oriGroup.Name}]绑定。\n" +
                                                   $"如需解绑原微信群并绑定新微信群，请在20秒内回复\"确定\"或\"取消\"！\n" +
                                                   $"回复\"确定\"后，将自动完成群内成员绑定的迁移。\n" +
                                                   $"请注意：\n" +
                                                   $"⚠️请尽量保证新群的成员与旧群的成员保持一致，新群内不存在的成员将丢失绑定信息；" +
                                                   $"⚠️请确保在迁移之前在新群内使用一次\"刷新群信息\"命令\n",
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
                                                                              result = context.ContactsM.MoveBindGroup(msg.Self, oriGroup.WXID, group.WXID, rid);
                                                                          }
                                                                          catch (Exception ex)
                                                                          {
                                                                              Context.Logger.Write(ex, _moveGroupTag);
                                                                          }
                                                                          if (result) context.WechatM.SendAtText($"俱乐部绑定迁移完成。\n" +
                                                                                                                 $"当前微信群已经成功与俱乐部俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]绑定。\n" +
                                                                                                                 $"原微信群[{oriGroup.Name}]中的所有绑定信息已被清空。\n" +
                                                                                                                 $"你可以发送\"查看俱乐部\"或\"查询微信群总览\"查看详情。",
                                                                                                                 new List<string> { msg.WXID },
                                                                                                                 msg.Self,
                                                                                                                 msg.Sender);
                                                                          else context.WechatM.SendAtText("因未知原因，操作失败了。",
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
                                                                      _moveGroupTag);
                        return;
                    }
                }


                var curRID = group.RID;
                if (!string.IsNullOrEmpty(curRID))
                {
                    if (curRID == rid)
                    {
                        context.WechatM.SendAtText($"当前微信群已经与俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]绑定，无需重复操作。\n",
                                                     new List<string> { msg.WXID },
                                                     msg.Self,
                                                     msg.Sender);
                        return;
                    }
                    // 当前群已经与其他俱乐部绑定
                    else
                    {
                        var curClub = context.ClubsM.FindClub(msg.Self, curRID);
                        context.WechatM.SendAtText($"当前微信群已经与其他俱乐部[{curClub?.Name ?? "新俱乐部"}-{curRID}]绑定。\n" +
                                                   $"⚠️你必须先将原俱乐部的绑定转移到其他群，或者撤销原俱乐部的绑定，之后才能继续操作！\n" +
                                                   $"⚠️若要转移原来的俱乐部，请通知该会长在其他群使用\"绑定群 {curRID}\"命令",
                                                     new List<string> { msg.WXID },
                                                     msg.Self,
                                                     msg.Sender);
                        return;
                    }
                }

                var result = context.ContactsM.BindGroup(msg.Self, msg.Sender, rid);
                if (result) context.WechatM.SendAtText($"当前微信群已经成功与俱乐部俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]绑定。\n" +
                                                       $"现在您可以继续进行[设置经理]和[设置成员]操作。",
                                                       new List<string> { msg.WXID },
                                                       msg.Self,
                                                       msg.Sender);
                else context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);

            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
            }
        }
    }
}
