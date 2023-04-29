using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd
{
    internal interface ICMD
    {
        public Context _context { get; set; }
        /// <summary>
        /// 命令名称
        /// </summary>
        public List<string> Commands { get; }
        public List<string> CommandsJP { get; }
        public List<string> CommandsQP { get; }
        /// <summary>
        /// 命令TAG
        /// </summary>
        public string Tag { get; }
        /// <summary>
        /// 适用聊天场景
        /// </summary>
        public include.ChatScene EnableScene { get; }
        /// <summary>
        /// 最低权限
        /// </summary>
        public include.UserRole MinRole { get; }
        /// <summary>
        /// 适用接受消息类型
        /// </summary>
        public RS.Tools.Common.Enums.WechatMessageType AcceptMessageType { get; }
        /// <summary>
        /// 执行消息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task Do(Message message);
    }
}
