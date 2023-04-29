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

    internal class cmd_qian_jie : ICMD
    {
        public Context _context { get; set; }
        public cmd_qian_jie(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "解签" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_qian_jie";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.NORMAL;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        async public Task Do(Message msg)
        {
            try
            {
                // 求签
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

                // 找到俱乐部
                var club = _context.ClubsM.FindClub(msg.Self, rid);
                if (club is null)
                {
                    _context.WechatM.SendAtText($"⚠️俱乐部[{rid}]不存在。",
                                                new List<string> { msg.WXID },
                                                msg.Self,
                                                msg.Sender);
                    return;
                }

                // 检查订购
                if (!CommonValidate.CheckPurchase(_context, msg, rid)) return;

                await Task.Run(() =>
                {
                    var result = _context.QianM.JieQian(msg.WXID);
                    if (string.IsNullOrEmpty(result)) _context.WechatM.SendAtText("⚠️未查询到任何信息。",
                                                                                new List<string> { msg.WXID },
                                                                                msg.Self,
                                                                                msg.Sender);
                    else _context.WechatM.SendAtText(result,
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
