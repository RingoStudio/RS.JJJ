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

namespace RS.Snail.JJJ.robot.cmd.misc
{

    internal class cmd_get_snail_bonus : ICMD
    {
        public Context _context { get; set; }
        public cmd_get_snail_bonus(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "蜗牛福利", "蜗牛签到", "蜗牛礼包" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_get_snail_bonus";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.PLAYER;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;

        public void Do(Message msg)
        {
            try
            {
                _context.WechatM.SendAtText($"每日签到{emoji.SHOUZHI}https://h5.qingcigame.com/package/template/enterco18d8ul98f?app_id=39\n\n" +
                                            $"青瓷小程序周礼包月礼包{emoji.SHOUZHI}#小程序://青瓷游戏Wiki/1PR4fQEp5MVCanp\n\n" +
                                            $"蜗牛公号周节日礼包{emoji.SHOUZHI}https://h5.qingcigame.com/package/index.html?id=39&area=QC-GAME\n\n" +
                                            $"青瓷公号关注礼包节日礼包{emoji.SHOUZHI}https://h5.qingcigame.com/package/index.html?id=39&area=QC-ZQWN\n\n" +
                                            $"官方充值每日每周每月积分兑换{emoji.SHOUZHI}https://pay.qingcigame.com/pay?gameId=39\n\n" +
                                            $"小路乖乖特工密令{emoji.SHOUZHI}#小程序://小乖乖蜗牛密令助手/NgcNCRXcbDZrtMH\n\n" +
                                            $"以上福利由咱们去捉泥鳅23服路总赞助播出",
                                       new List<string> { msg.Sender }, msg.RoomID);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, Tag);
            }
        }
    }
}
