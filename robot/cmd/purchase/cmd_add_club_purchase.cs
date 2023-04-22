using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.purchase
{
    [attribute.CmdClass]
    internal class cmd_add_club_purchase
    {
        public const string Instrus = "新增订阅";
        public const string Tag = "cmd_add_club_purchase";
        public const include.ChatScene EnableScene = include.ChatScene.All;
        public const include.UserRole MinRole = include.UserRole.ADMINISTRATOR;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                // 新增订阅 [OPT:RID] [月数]
                var rid = "";
                var arr = msg.ExplodeContent;
                var add = 0;
                if (arr.Length > 1)
                {
                    for (int i = 1; i <= arr.Length; i++)
                    {
                        if (StringHelper.IsRID(arr[i])) rid = arr[i];
                        else if (StringHelper.IsInt(arr[i])) add = Convert.ToInt32(arr[i]);
                    }
                }

                if (add == 0) return;

                // 未指定rid，则为本群rid
                if (string.IsNullOrEmpty(rid))
                {
                    if (msg.Scene == ChatScene.Private) return;
                    else
                    {
                        var group = context.ContactsM.FindGroup(msg.Self, msg.Sender);
                        if (group is null)
                        {
                            context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
                                                        new List<string> { msg.WXID },
                                                        msg.Self,
                                                        msg.Sender);
                            return;
                        }
                        rid = group.RID;
                    }
                }

                if (string.IsNullOrEmpty(rid)) return;


                // 找到俱乐部
                var club = context.ClubsM.FindClub(msg.Self, rid);
                if (club is null)
                {
                    context.WechatM.SendAtText($"⚠️要查询的俱乐部[{rid}]不存在。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                if (add < 0)
                {
                    club.PurchaseEnd = long.MinValue;
                    var desc = $"已将俱乐部[{club.Name}]的唧唧叽订阅期限设置为[永久]";
                    context.WechatM.SendAtText(desc,
                                          new List<string> { msg.WXID },
                                          msg.Self,
                                          msg.Sender);
                }
                else
                {
                    club.PurchaseEnd = TimeHelper.AddMonth(club.PurchaseEnd, add);
                    var desc = $"已将俱乐部[{club.Name}]的唧唧叽订阅增加了{add}个月，订阅期限延长至[{TimeHelper.ChinsesTimeDesc(club.PurchaseEnd)}]";
                    context.WechatM.SendAtText(desc,
                                               new List<string> { msg.WXID },
                                               msg.Self,
                                               msg.Sender);
                }
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
            }
        }
    }
}
