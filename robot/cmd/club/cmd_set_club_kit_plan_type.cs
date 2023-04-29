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

    internal class cmd_set_club_kit_plan_type : ICMD
    {
        public Context _context { get; set; }
        public cmd_set_club_kit_plan_type(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "设置套装分配模式", "设置套装分配模式" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_set_club_kit_plan_type";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.GROUP_MANAGER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        async public Task Do(Message msg)
        {
            try
            {
                // 设置套装分配模式 [OPT:RID] [战力/领导力/兵种实力]
                var rid = "";
                var arr = msg.ExplodeContent;
                var mode = include.ClubKitPlanType.UNDEFINED;
                if (arr.Length > 1)
                {
                    for (int i = 1; i < arr.Length; i++)
                    {
                        if (StringHelper.IsRID(arr[i])) rid = arr[i];
                        else
                        {
                            var instru = arr[i].Replace("按", "");
                            switch (instru)
                            {
                                case "战力":
                                case "实力":
                                case "综合实力":
                                    mode = ClubKitPlanType.COMBAT;
                                    break;
                                case "领导力":
                                    mode = ClubKitPlanType.LEADERSHIP;
                                    break;
                                default:
                                    mode = ClubKitPlanType.SONCOMBAT;
                                    break;
                            }
                        }
                    }
                }

                if (mode == ClubKitPlanType.UNDEFINED) return;

                // 未指定rid，则为本群rid
                if (string.IsNullOrEmpty(rid))
                {
                    if (msg.Scene == ChatScene.Private) return;
                    else
                    {
                        var group = _context.ContactsM.FindGroup(msg.Self, msg.Sender);
                        if (group is null)
                        {
                            _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
                                                        new List<string> { msg.WXID },
                                                        msg.Self,
                                                        msg.Sender);
                            return;
                        }
                        rid = group.RID;
                    }
                }

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

                club.ClubKitPlanType = mode;
                var desc = $"已将俱乐部[{club.Name}]的套装分配模式设置为[{include.club.ClubKitPlanTypeDesc(mode)}]\n" +
                           $"通常是每周五通过\"生成布阵图\"指令分配套装\n" +
                           $"若该俱乐部周五未分配套装，则之后的物种周内允许重新分配一次";
                _context.WechatM.SendAtText(desc,
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
