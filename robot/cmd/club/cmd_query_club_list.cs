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

    internal class cmd_query_club_list : ICMD
    {
        public Context _context { get; set; }
        public cmd_query_club_list(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "查询俱乐部名单" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_query_club_list";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        async public Task Do(Message msg)
        {
            try
            {
                var result = await Task.Run(() => _context.ClubsM.GetClubListExcel(msg.Self));
                if (string.IsNullOrEmpty(result) || !System.IO.File.Exists(result))
                {
                    _context.WechatM.SendAtText("⚠️因未知原因，操作失败了。",
                                            new List<string> { msg.WXID },
                                            msg.Self,
                                            msg.Sender);
                }
                else
                {
                    _context.WechatM.SendFile(result, msg.Self, msg.Sender);

                }
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
