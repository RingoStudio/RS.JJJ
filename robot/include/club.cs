using RS.Tools.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.include
{
    internal class club
    {
        public static string ClubKitPlanTypeDesc(ClubKitPlanType type) => type switch
        {
            ClubKitPlanType.SONCOMBAT => "按兵演战力",
            ClubKitPlanType.LEADERSHIP => "按领导力",
            ClubKitPlanType.COMBAT => "按本体战力",
            _ => "未设定",
        };

        public static string ChannelTypeDesc(ChannelType channelType) => channelType switch
        {
            ChannelType.IOS => "iOS官服",
            ChannelType.ANDROID => "安卓官服",
            ChannelType.CHANNEL => "渠道服",
            ChannelType.GUANGZI => "光子服",
            _ => "其他服",
        };

        public static string KitDesc(Kit kit) => kit switch
        {
            Kit.GANSI => "敢死",
            Kit.KANTAN => "勘探",
            Kit.KULI => "苦力",
            _ => "未指定",
        };
    }

    internal enum Kit
    {
        UNDEFINED = 0,
        GANSI = 1,
        KANTAN = 2,
        KULI = 3,
    }

    internal enum ClubKitPlanType
    {
        UNDEFINED = 0,
        LEADERSHIP = 1,
        COMBAT = 2,
        SONCOMBAT = 3,
    }

    internal enum TokenQueryType
    {
        /// <summary>
        /// 未用和未领
        /// </summary>
        DEFAULT = 0,
        LACK = 1,
        // LACK_TOTAL = 2,
        NOT_USE = 3,
        BUY = 4,
        NOT_BUY = 5,
    }

    internal enum DrillQueryType
    {
        DEFAULT = 0,
        NOT_USE = 1,
        LACK = 2,
    }
}
