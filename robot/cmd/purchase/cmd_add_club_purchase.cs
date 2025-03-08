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

namespace RS.Snail.JJJ.robot.cmd.purchase
{

    internal class cmd_add_club_purchase : ICMD
    {
        public Context _context { get; set; }
        public cmd_add_club_purchase(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "新增订阅", "增加订阅" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_add_club_purchase";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        public void Do(Message msg)
        {
            try
            {
                // 新增订阅 [OPT:RID] [月数]
                var rid = "";
                var arr = msg.ExplodeContent;
                char type = 'O';
                var add = 0;
                if (arr.Length > 1)
                {
                    for (int i = 1; i < arr.Length; i++)
                    {
                        if (StringHelper.IsRID(arr[i]) && string.IsNullOrEmpty(rid)) rid = arr[i];
                        else if (StringHelper.IsInt(arr[i])) add = Convert.ToInt32(arr[i]);
                        else
                        {
                            var num = arr[i].Substring(0, arr[i].Length - 1);
                            var mark = char.ToUpper(arr[i].Last());
                            if (StringHelper.IsNumeric(num) && "YMWDHIC".Contains(mark))
                            {
                                type = mark;
                                add = Convert.ToInt32(num);
                            }
                        }
                    }
                }

                if (!"YMWDHIC".Contains(type)) return;

                // if (add == 0) return;

                // 未指定rid，则为本群rid
                if (string.IsNullOrEmpty(rid))
                {
                    if (msg.Scene == ChatScene.Private) return;
                    else
                    {
                        var group = _context.ContactsM.FindGroup(msg.RoomID);
                        if (group is null)
                        {
                            _context.WechatM.SendAtText($"⚠️唧唧叽缺少当前微信群的资料，请联系超管使用命令\"刷新群信息\"。",
                                                        new List<string> { msg.Sender }, msg.RoomID);
                            return;
                        }
                        rid = group.RID;
                    }
                }

                if (string.IsNullOrEmpty(rid)) return;


                // 找到俱乐部
                var club = _context.ClubsM.FindClub(rid);
                if (club is null)
                {
                    _context.WechatM.SendAtText($"⚠️要查询的俱乐部[{rid}]不存在。",
                                                new List<string> { msg.Sender }, msg.RoomID);
                    return;
                }

                club.PurchaseEnd = type switch
                {
                    'Y' => TimeHelper.AddYears(club.PurchaseEnd, add),
                    'M' => TimeHelper.AddMonths(club.PurchaseEnd, add),
                    'W' => TimeHelper.AddWeeks(club.PurchaseEnd, add),
                    'D' => TimeHelper.AddDays(club.PurchaseEnd, add),
                    'I' => long.MinValue,
                    'C' => TimeHelper.ToTimeStamp() - 3600,
                    _ => club.PurchaseEnd,
                };

                string desc;
                if (type == 'I')
                {
                    desc = $"已将俱乐部[{club.Name}]的唧唧叽订阅期限设置为[无限]";
                }
                if (type == 'C')
                {
                    desc = $"已将俱乐部[{club.Name}]的唧唧叽订阅期限清空";
                }
                else
                {
                    if (add == 0) return;
                    type = type switch
                    {
                        'Y' => '年',
                        'W' => '周',
                        'D' => '天',
                        _ => '月',
                    };
                    desc = $"已将俱乐部[{club.Name}]的唧唧叽订阅增加了{add}{type}，订阅期限延长至[{TimeHelper.ChinsesTimeDesc(club.PurchaseEnd)}]";
                }
                _context.WechatM.SendAtText(desc, new List<string> { msg.Sender }, msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
