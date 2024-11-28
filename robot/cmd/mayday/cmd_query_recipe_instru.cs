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

    internal class cmd_query_recipe_instru : ICMD
    {
        public Context _context { get; set; }
        public cmd_query_recipe_instru(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "处方字段说明", "处方生成说明", "处方生成文本", "处方文字说明" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_query_recipe_instru";
        public ChatScene EnableScene => ChatScene.Group;
        public UserRole MinRole => UserRole.MAYDAY;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        public void Do(Message msg)
        {
            try
            {
                if (_context.MaydayM.IsBanned(msg.Sender)) return;

                var content = _context.MaydayM.QueryRecipeInstru();
                if (content.Length <= 200 || _context.IsTest)
                {
                    _context.WechatM.SendAtText(content, new List<string> { msg.Sender }, msg.RoomID);
                }
                else
                {
                    var fileName = $"OUT\\处方文字说明_@{_context.ContactsM.QueryGroupMemberNick(msg.Sender, msg.RoomID)}_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.txt";
                    fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                    System.IO.File.WriteAllText(fileName, content);
                    _context.WechatM.SendFile(fileName, msg.RoomID);
                }
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
