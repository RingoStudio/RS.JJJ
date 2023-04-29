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
    internal class cmd_add_club : ICMD
    {
        public Context _context { get; set; }
        public cmd_add_club(Context context)
        {
            _context = context;
        }
        public List<string> Commands { get; } = new List<string> { "增加俱乐部" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag { get; } = "cmd_add_club";
        public ChatScene EnableScene { get; } = include.ChatScene.All;
        public UserRole MinRole { get; } = include.UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType { get; } = Tools.Common.Enums.WechatMessageType.Text;

        async public Task Do(Message msg)
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

                var club = _context.ClubsM.FindClub(msg.Self, rid);
                if (club is not null)
                {
                    _context.WechatM.SendAtText($"⚠️已存在俱乐部: {club.Name} [{club.RID}]，添加失败！",
                                                   new List<string> { msg.WXID },
                                                   msg.Self,
                                                   msg.Sender);

                    return;
                }

                var result = _context.ClubsM.AddClub(msg.Self, rid, channelType);
                if (!result) _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
                                                         new List<string> { msg.WXID },
                                                         msg.Self,
                                                         msg.Sender);
                else _context.WechatM.SendAtText($"⚠️已成功添加俱乐部 [{rid} {include.club.ChannelTypeDesc(channelType)}]。\n" +
                                                 $"请立即设置会长和登录账号。", new List<string> { msg.WXID }, msg.Self, msg.Sender);
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
