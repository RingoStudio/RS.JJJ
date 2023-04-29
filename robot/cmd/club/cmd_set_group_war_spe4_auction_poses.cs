using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.cmd.utils;
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

    internal class cmd_set_group_war_spe4_auction_poses : ICMD
    {
        public Context _context { get; set; }
        public cmd_set_group_war_spe4_auction_poses(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "设置仓鼠拍卖地块", "设置仓鼠拍卖位置", "设置仓鼠拍卖指令" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_set_group_war_spe4_auction_poses";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        async public Task Do(Message msg)
        {
            try
            {
                // 设置仓鼠拍卖地块 [POS1] ...
                var arr = msg.ExplodeContent;
                List<string> poses = new List<string>();
                if (arr.Length > 1)
                {
                    for (int i = 1; i <= arr.Length; i++)
                    {
                        if (!poses.Contains(arr[i])) poses.Add(arr[i]);
                    }
                }


                // 未指定rid，则为本群rid

                var group = _context.ContactsM.FindGroup(msg.Self, msg.Sender);
                if (group is null)
                {
                    _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }
                var rid = group.RID;

                if (string.IsNullOrEmpty(rid)) return;

                // 检查本俱乐部权限
                if (!_context.ContactsM.CheckGroupRole(msg.Self, rid, msg.WXID, msg.Scene == ChatScene.Group ? msg.Sender : ""))
                {
                    _context.WechatM.SendAtText($"不可以查看其他俱乐部的信息。",
                                             new List<string> { msg.WXID },
                                             msg.Self,
                                             msg.Sender);
                    return;
                }

                // 找到俱乐部
                var club = _context.ClubsM.FindClub(msg.Self, rid);
                if (club is null)
                {
                    _context.WechatM.SendAtText($"⚠️要查询的俱乐部[{rid}]不存在。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;

                await Task.Run(() =>
                {
                    if (poses.Count > 0)
                    {
                        var list = _context.ClubsM.GetGroupWarSpe4AuctionPoses(msg.Self, rid);
                        if (list.Count == 0)
                        {

                            _context.WechatM.SendAtText($"⚠️还没有获取到今日可拍卖地块，请确定今天是仓鼠拍卖日，请先登录一次再试",
                                                        new List<string> { msg.WXID },
                                                        msg.Self,
                                                        msg.Sender);
                            return;
                        }
                        foreach (var item in poses)
                        {
                            if (!list.Contains(item))
                            {
                                _context.WechatM.SendAtText($"⚠️今日可拍卖的地块包括 [{string.Join("、", list)}]\n你只能输入以上任意地块，并在命令后面使用空格割开",
                                                            new List<string> { msg.WXID },
                                                            msg.Self,
                                                            msg.Sender);
                                return;
                            }
                        }

                    }


                    var result = _context.ClubsM.SetGroupWarSpe4AuctionSetPoses(msg.Self, rid, poses);

                    _context.WechatM.SendAtText($"⚠️{result}",
                                                   new List<string> { msg.WXID },
                                                   msg.Self,
                                                   msg.Sender);
                });
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
            }
        }
    }
}
