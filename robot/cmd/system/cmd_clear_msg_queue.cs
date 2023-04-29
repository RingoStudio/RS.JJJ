using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.system
{

    internal class cmd_clear_msg_queue : ICMD
    {
        public Context _context { get; set; }
        public cmd_clear_msg_queue(Context context)
        {
            _context = context;
        }
        public List<string> Commands => new List<string> { "/清理消息队列", "/清理消息", "/clearmsgqueue" };
        public List<string> CommandsJP { get => Commands.Select(a => Pinyin.GetInitials(a).ToLower()).ToList(); }
        public List<string> CommandsQP { get => Commands.Select(a => Pinyin.GetPinyin(a).ToLower()).ToList(); }
        public string Tag => "cmd_clear_msg_queue";
        public ChatScene EnableScene => ChatScene.All;
        public UserRole MinRole => UserRole.ADMINISTRATOR;
        public WechatMessageType AcceptMessageType => WechatMessageType.Text;
        async public Task Do(Message msg)
        {
            await Task.Run(() =>
              {
                  try
                  {
                      var arr = msg.ExplodeContent;
                      var isAll = false;
                      if (arr.Length > 1 && (arr[1].ToLower() == "all" || arr[1] == "全部" || arr[1] == "所有")) isAll = true;

                      if (isAll)
                      {
                          _context.WechatM.ClearMessageQueue();
                          _context.WechatM.SendMgrNotice($"{include.emoji.ZHUYI}已清空待发送的消息队列，已在队列内的回复将全部取消");
                      }
                      else
                      {
                          _context.WechatM.ClearMessageQueue(msg.Self);
                          _context.WechatM.SendMgrNotice($"{include.emoji.ZHUYI}已清空待发送的消息队列，已在队列内的回复将全部取消", msg.Self);
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
              });

        }
    }
}
