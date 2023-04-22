using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RS.Tools.Common.Enums;

namespace RS.Snail.JJJ.boot
{
    internal readonly struct CommandInfo
    {
        public string? Desc { get; }
        public List<string>? Instrus { get; }
        public List<string>? InstrusJianpin { get; }
        public MethodInfo MethodInfo { get; }

        public RS.Tools.Common.Enums.WechatMessageType? AcceptType { get; }
        public robot.include.ChatScene? EnableScene { get; }
        public robot.include.UserRole? MinRole { get; }
        public Type? InstanceType { get; }

        public InvokeType InvokeType { get; }

        public CommandInfo(string? desc, MethodInfo methodInfo, Type? instanceType,
                           List<string>? instrus = null, List<string>? instrusJianpin = null,
                           robot.include.ChatScene? enableScene = robot.include.ChatScene.All,
                           robot.include.UserRole? minRole = robot.include.UserRole.NORMAL,
                           RS.Tools.Common.Enums.WechatMessageType? acceptType = RS.Tools.Common.Enums.WechatMessageType.Text)
        {
            Desc = desc;
            MethodInfo = methodInfo;
            InstanceType = instanceType;
            InvokeType = Tools.Common.Enums.InvokeType.Method;
            Instrus = instrus;
            InstrusJianpin = instrusJianpin;
            EnableScene = enableScene;
            MinRole = minRole;
            AcceptType = acceptType;
        }
    }
}
