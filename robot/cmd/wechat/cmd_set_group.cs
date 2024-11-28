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

namespace RS.Snail.JJJ.robot.cmd.wechat
{

    internal class cmd_set_group : ICMD
    {
        public Context _context { get; set; }
        public cmd_set_group(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "绑定群" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_set_group";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.GROUP_HOLDER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        private static string _moveGroupTag = "cmd_set_group_move";


        public void Do(Message msg)
        {
            try
            {
                _context.CommunicateM.UnregistWaitMessageRequest(msg.RoomID, msg.Sender, _moveGroupTag);

                var arr = msg.ExplodeContent;

                if (arr.Length < 2) return;

                // 解析rid
                var rid = arr.Last();
                if (!StringHelper.IsRID(rid)) return;

                // 找到俱乐部
                var club = _context.ClubsM.FindClub(rid);

                if (club is null)
                {
                    _context.WechatM.SendAtText($"⚠️要设置的俱乐部[{rid}]不存在。", new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                var curHolder = _context.ContactsM.QueryClubHolderWXID(rid);
                if (string.IsNullOrEmpty(curHolder) || curHolder != msg.Sender)
                {
                    _context.WechatM.SendAtText($"⚠️你目前不是俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]的会长。\n" +
                                                $"⚠️此命令必须由当前俱乐部会长本人操作。",
                                                new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                var group = _context.ContactsM.FindGroup(msg.RoomID);
                if (group is null)
                {
                    _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
                                                new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }


                var oriGroup = _context.ContactsM.FindGroupByRID(rid);
                if (oriGroup is not null)
                {
                    if (oriGroup.WXID == group.WXID)
                    {
                        _context.WechatM.SendAtText($"当前微信群已经与俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]绑定，无需重复操作。\n",
                                                    new List<string> { msg.Sender }, msg.RoomID);
                        return;
                    }
                    // 当前俱乐部已经绑定在其他群
                    else
                    {

                        _context.WechatM.SendAtText($"当前俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]已经与其他微信群[{oriGroup.Name}]绑定。\n" +
                                                    $"如需解绑原微信群并绑定新微信群，请在20秒内回复\"确定\"或\"取消\"！\n" +
                                                    $"回复\"确定\"后，将自动完成群内成员绑定的迁移。\n" +
                                                    $"请注意：\n" +
                                                    $"⚠️请尽量保证新群的成员与旧群的成员保持一致，新群内不存在的成员将丢失绑定信息；" +
                                                    $"⚠️请确保在迁移之前在新群内使用一次\"刷新群信息\"命令\n",
                                                    new List<string> { msg.Sender }, msg.RoomID);
                        _context.CommunicateM.RegistWaitMessageRequest(msg.RoomID, msg.Sender,
                                                                       onReceivedCallback: new Action<Message>((_msg) =>
                                                                          {
                                                                              if (_msg.Content != "确定") return;
                                                                              var result = false;
                                                                              try
                                                                              {
                                                                                  result = _context.ContactsM.MoveBindGroup(oriGroup.WXID, group.WXID, rid);
                                                                              }
                                                                              catch (Exception ex)
                                                                              {
                                                                                  Context.Logger.WriteException(ex, _moveGroupTag);
                                                                              }
                                                                              if (result) _context.WechatM.SendAtText($"俱乐部绑定迁移完成。\n" +
                                                                                                                      $"当前微信群已经成功与俱乐部俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]绑定。\n" +
                                                                                                                      $"原微信群[{oriGroup.Name}]中的所有绑定信息已被清空。\n" +
                                                                                                                      $"你可以发送\"查看俱乐部\"或\"查询微信群总览\"查看详情。",
                                                                                                                      new List<string> { msg.Sender }, msg.RoomID);
                                                                              else _context.WechatM.SendAtText("因未知原因，操作失败了。", new List<string> { msg.Sender }, msg.RoomID);
                                                                          }),
                                                                      verifier: new Func<Message, bool>((_msg) =>
                                                                      {
                                                                          return _msg.Content == "确定" || _msg.Content == "取消";
                                                                      }),
                                                                      waitSeconds: 20,
                                                                      tag: _moveGroupTag);
                        return;
                    }
                }


                var curRID = group.RID;
                if (!string.IsNullOrEmpty(curRID))
                {
                    if (curRID == rid)
                    {
                        _context.WechatM.SendAtText($"当前微信群已经与俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]绑定，无需重复操作。\n", new List<string> { msg.Sender }, msg.RoomID);
                        return;
                    }
                    // 当前群已经与其他俱乐部绑定
                    else
                    {
                        var curClub = _context.ClubsM.FindClub(curRID);
                        _context.WechatM.SendAtText($"当前微信群已经与其他俱乐部[{curClub?.Name ?? "新俱乐部"}-{curRID}]绑定。\n" +
                                                    $"⚠️你必须先将原俱乐部的绑定转移到其他群，或者撤销原俱乐部的绑定，之后才能继续操作！\n" +
                                                    $"⚠️若要转移原来的俱乐部，请通知该会长在其他群使用\"绑定群 {curRID}\"命令",
                                                    new List<string> { msg.Sender }, msg.RoomID);
                        return;
                    }
                }

                var result = _context.ContactsM.BindGroup(msg.RoomID, rid);
                if (result) _context.WechatM.SendAtText($"当前微信群已经成功与俱乐部俱乐部[{club?.Name ?? "新俱乐部"}-{rid}]绑定。\n" +
                                                        $"现在您可以继续进行[设置经理]和[设置成员]操作。",
                                                        new List<string> { msg.Sender }, msg.RoomID);
                else _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
                                                 new List<string> { msg.Sender }, msg.RoomID);

            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
