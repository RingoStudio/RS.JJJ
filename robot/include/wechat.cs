using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.include
{
    public class wechat
    {
        public static string UserRoleDesc(UserRole role) => role switch
        {
            UserRole.ADMINISTRATOR => "超管",
            UserRole.GROUP_HOLDER => "会长",
            UserRole.GROUP_MANAGER => "经理",
            UserRole.NORMAL => "正式成员",
            UserRole.PLAYER => "见习",
            UserRole.NONE or UserRole.UNDEFINED => "无",
            _ => "超管",
        };
    }

    /// <summary>
    /// 用户权限
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// 机器人自身
        /// </summary>
        BOT = 9999,
        ADMINISTRATOR = 999,
        GROUP_HOLDER = 99,
        GROUP_MANAGER = 9,
        NORMAL = 5,
        PLAYER = 1,
        NONE = 0,
        UNDEFINED = -1,
    }

    /// <summary>
    /// 聊天场景
    /// </summary>
    internal enum ChatScene
    {
        All = 0,
        Group = 1,
        Private = 2,

        UNDEFINDE = -1,
    }
}
