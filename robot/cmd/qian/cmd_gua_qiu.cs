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
    
    internal class cmd_gua_qiu : ICMD
    {
        public Context _context { get; set; }
        public cmd_gua_qiu(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "求卦", "六爻" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_gua_qiu";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.NORMAL;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        async public Task Do(Message msg)
        {
            try
            {
                // 求卦 [int]
                int number = 0;
                var arr = msg.ExplodeContent;
                if (arr.Length < 2) return;
                if (!StringHelper.IsInt(arr[1])) return;
                number = Convert.ToInt32(arr[1]);

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
                    var result = _context.QianM.QiuGua(msg.WXID, number);
                    if (string.IsNullOrEmpty(result)) _context.WechatM.SendAtText("⚠️未查询到任何信息。",
                                                                                new List<string> { msg.WXID },
                                                                                msg.Self,
                                                                                msg.Sender);
                    else
                    {
                        if (result.Length <= 200 || _context.IsTest)
                        {
                            _context.WechatM.SendAtText(result,
                                                    new List<string> { msg.WXID },
                                                    msg.Self,
                                                    msg.Sender);
                        }
                        else
                        {
                            var fileName = $"OUT\\解卦_{_context.ContactsM.QueryGroupMemberNick(msg.WXID, msg.Self, msg.Sender)}_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.txt";
                            fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                            System.IO.File.WriteAllText(fileName, result);
                            _context.WechatM.SendFile(fileName, msg.Self, msg.Sender);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, Tag);
            }
        }
    }
}
