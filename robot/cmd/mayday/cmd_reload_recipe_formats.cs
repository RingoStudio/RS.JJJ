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

    internal class cmd_reload_recipe_formats : ICMD
    {
        public Context _context { get; set; }
        public cmd_reload_recipe_formats(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "重载处方格式", "刷新处方格式" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_reload_recipe_formats";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        public void Do(Message msg)
        {
            try
            {
                if (!_context.MaydayM.LoadRecipeFormat()) _context.WechatM.SendAtText($"⚠️重载处方格式失败！", new List<string> { msg.Sender }, msg.RoomID);
                else _context.WechatM.SendAtText($"重载处方格式成功！", new List<string> { msg.Sender }, msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
