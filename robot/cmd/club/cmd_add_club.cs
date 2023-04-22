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
    [attribute.CmdClass]
    internal class cmd_add_club
    {
        public const string Instrus = "增加俱乐部";
        public const string Tag = "cmd_add_club";
        public const include.ChatScene EnableScene = include.ChatScene.All;
        public const include.UserRole MinRole = include.UserRole.ADMINISTRATOR;
        public const Tools.Common.Enums.WechatMessageType AcceptMessageType = Tools.Common.Enums.WechatMessageType.Text;

        [attribute.Cmd(Name: Tag, instru: Instrus, enableScene: (int)EnableScene, minRole: (int)MinRole, acceptType: (int)AcceptMessageType)]
        public static void Do(Context context, Message msg)
        {
            try
            {
                // 增加俱乐部 [RID] [CHANNEL]
                var arr = msg.ExplodeContent;
                if (arr.Length < 2) return;

                var rid = arr[1];
                if (!StringHelper.IsRID(rid)) return;

                var channelType = ChannelType.IOS;
                if (arr.Length >= 3)
                {
                    var channel = arr[2].ToLower();
                    switch (channel)
                    {
                        case "ios":
                        case "苹果":
                            channelType = ChannelType.IOS;
                            break;
                        case "android":
                        case "安卓":
                            channelType = ChannelType.ANDROID;
                            break;
                        default:
                            break;
                    }
                }

                var club = context.ClubsM.FindClub(msg.Self, rid);
                if (club is not null)
                {
                    context.WechatM.SendAtText($"⚠️已存在俱乐部: {club.Name} [{club.RID}]，添加失败！",
                                                   new List<string> { msg.WXID },
                                                   msg.Self,
                                                   msg.Sender);

                    return;
                }

                var result = context.ClubsM.AddClub(msg.Self, rid, channelType);
                if (!result)
                {
                    context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                }
                else
                {
                    context.WechatM.SendAtText($"⚠️已成功添加俱乐部 [{rid} {include.club.ChannelTypeDesc(channelType)}]。\n" +
                                               $"请立即设置会长和登录账号。",
                                               new List<string> { msg.WXID },
                                               msg.Self,
                                               msg.Sender);
                }
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
                context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
            }
        }
    }
}
