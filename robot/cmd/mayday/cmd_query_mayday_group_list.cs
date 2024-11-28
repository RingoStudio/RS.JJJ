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

namespace RS.Snail.JJJ.robot.cmd.mayday
{

    internal class _query_mayday_group_list : ICMD
    {
        public Context _context { get; set; }
        public _query_mayday_group_list(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "查询五迷群列表" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "_query_mayday_group_list";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        public void Do(Message msg)
        {
            try
            {
                var dic = _context.MaydayM.QueryMaydayGroupList();
                var list = new List<string>();
                if (dic.Count > 0)
                {
                    list.Add($"五迷群共有以下{dic.Count}个：");
                    list.AddRange(dic.Values);
                    if (msg.IsGroup)
                    {
                        if (dic.ContainsKey(msg.RoomID)) list.Add($"{emoji.HAHA}本群就是五迷群哦~");
                        else list.Add($"{emoji.HAN}很遗憾，本群目前还不是五迷群");
                    }
                }
                else
                {
                    list.Add($"没有添加任何五迷群。");
                }
                var desc = string.Join("\n", list);

                if (desc.Length > 200)
                {
                    var fileName = $"OUT\\五迷群列表查询结果_@{_context.ContactsM.QueryGroupMemberNickForFile(msg.Sender, msg.RoomID)}_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.txt";
                    fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                    System.IO.File.WriteAllText(fileName, desc);
                    _context.WechatM.SendFile(fileName, msg.RoomID);
                }
                else _context.WechatM.SendAtText(desc, new List<string> { msg.Sender }, msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
