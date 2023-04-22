using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.attribute
{
    /// <summary>
    /// 可执行指令Attrib
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class CmdAttribute : RSBaseAttribute
    {
        private string[]? _jianpin = null;
        /// <summary>
        /// 命令全文
        /// </summary>
        public string[]? Instru;
        /// <summary>
        /// 命令简拼
        /// </summary>
        public string[]? NameJianPin { get => _jianpin; }
        /// <summary>
        /// 适用场景：私聊/群聊
        /// </summary>
        public robot.include.ChatScene? EnableScene { get; private set; }
        /// <summary>
        /// 最低权限
        /// </summary>
        public robot.include.UserRole? MinRole { get; private set; }
        /// <summary>
        /// 适用消息类型
        /// </summary>
        public RS.Tools.Common.Enums.WechatMessageType? AcceptType { get; private set; }


        public CmdAttribute(string Name, string instru,
                            int enableScene = 0,
                            int minRole = 5,
                            int acceptType = 1) : base(Name)
        {
            EnableScene = (robot.include.ChatScene)enableScene;
            MinRole = (robot.include.UserRole)minRole;
            AcceptType = (RS.Tools.Common.Enums.WechatMessageType)acceptType;
            if (!string.IsNullOrEmpty(instru))
            {
                Instru = instru.Split(",");
                _jianpin = Instru.ToList().Select((a) => RS.Tools.Common.Utils.Pinyin.GetInitials(a).ToLower()).ToArray();
            }
        }
    }
}
