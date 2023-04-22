using RS.Snail.JJJ.attribute;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.boot
{
    internal class CommandManager
    {
        public CommandManager() { }

        #region PRIVATE FIELDS
        private readonly Dictionary<Type, dynamic> _instanceDict = new();
        #endregion
        public dynamic Invoke(CommandInfo commandInfo, params object[]? param)
        {
            return commandInfo.MethodInfo.Invoke(commandInfo.InstanceType is null ? null : _instanceDict[commandInfo.InstanceType], param);

        }
        public void InvokeAction(CommandInfo commandInfo, params object[]? param)
        {
            commandInfo.MethodInfo.Invoke(commandInfo.InstanceType is null ? null : _instanceDict[commandInfo.InstanceType], param);
        }
        public Dictionary<string, CommandInfo> MappingMethodsForCMD()
        {
            var attributeGroupType = typeof(attribute.CmdClassAttribute);
            var attribMethodType = typeof(attribute.CmdAttribute);
            var cmdGroups = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes().Where(type => type.IsDefined(attributeGroupType, false) && type.IsClass)
                        .Select(type => (type, type.GetMethods()))
                        .ToArray())
                    .ToArray();
            var result = new Dictionary<string, CommandInfo>();
            foreach (var cmd in cmdGroups)
            {
                var classType = cmd.type;
                foreach (var methodInfo in cmd.Item2)
                {
                    try
                    {
                        if (GenerateCommandInfoForCMD(methodInfo, attribMethodType, classType, out var commandInfo) is null) continue;
                        result.TryAdd(commandInfo.Desc, commandInfo);
                    }
                    catch (Exception e)
                    {
                        Logger.Instance.Write(e, "MappingMethods");
                    }
                }
            }
            return result;
        }

        private Attribute GenerateCommandInfoForCMD(MethodInfo method, Type attributeType, Type classType, out CommandInfo commandInfo)
        {
            //获取指令属性
            var commandAttr = method.GetCustomAttribute(attributeType);

            //检查和创建实例
            //若创建实例失败且方法不是静态的，则返回空白命令信息
            if (commandAttr is null || !method.IsStatic && !CheckAndCreateInstance(classType))
            {
                commandInfo = ObjectHelper.CreateInstance<CommandInfo>();
                return null;
            }

            var attribute = commandAttr as attribute.CmdAttribute;
            //创建指令信息
            commandInfo = new CommandInfo(attribute?.Name,
                                          method,
                                          (method.IsStatic ? null : classType),
                                          attribute?.Instru?.ToList(),
                                          attribute?.NameJianPin?.ToList(),
                                          attribute?.EnableScene,
                                          attribute?.MinRole,
                                          attribute?.AcceptType);


            return commandAttr;
        }
        /// <summary>
        /// 检查实例的存在和生成
        /// </summary>
        /// <param name="classType"></param>
        /// <returns></returns>
        private bool CheckAndCreateInstance(Type classType)
        {
            //获取类属性
            if (!classType?.IsClass ?? true)
            {
                Logger.Instance.WriteInfo("Command", "method reflected objcet is not a class");
                return false;
            }

            //检查是否已创建过实例
            if (_instanceDict.Any(ins => ins.Key == classType)) return true;

            try
            {
                //创建实例
                var instance = classType.CreateInstance();

                //添加实例
                _instanceDict
                    .Add(classType ?? throw new ArgumentNullException(nameof(classType), "get null class type"),
                         instance);
            }
            catch (Exception e)
            {
                Logger.Instance.Write(e, "Command: cannot create instance with error:");
                return false;
            }

            return true;
        }
    }
}
