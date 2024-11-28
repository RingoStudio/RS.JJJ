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
        public static string[] FiveAttribs = new[]{
                                                    "charisma", // 艺术
                                                    "wit",      // 文化
                                                    "luck",     //信仰
                                                    "popularity", // 人气
                                                    "knowledge",// 科技
                                                  };
        public static string[] FiveAttribs2 = new[] {
                                                        "influence",
                                                        "art",
                                                        "religion",
                                                        "culture",
                                                        "technology",
                                                    };
        public static string FiveAttribDesc(string attrib) => attrib switch
        {
            "charisma" or "art" => "艺术",
            "wit" or "culture" => "文化",
            "luck" or "religion" => "信仰",
            "popularity" or "influence" => "人气",
            "knowledge" or "technology" => "科技",
            _ => "",
        };

        public static string ClubKitPlanTypeDesc(ClubKitPlanType type) => type switch
        {
            ClubKitPlanType.SONCOMBAT => "按兵演战力",
            ClubKitPlanType.LEADERSHIP => "按领导力",
            ClubKitPlanType.COMBAT => "按本体战力",

            ClubKitPlanType.MAX_HP => "按演练生命",
            ClubKitPlanType.ATTACK => "按演练攻击",
            ClubKitPlanType.DEFENSE => "按演练防御",
            ClubKitPlanType.COMBO => "按演练追击",

            ClubKitPlanType.CHARISMA => "按演练艺术",
            ClubKitPlanType.WIT => "按演练文化",
            ClubKitPlanType.LUCK => "按演练信仰",
            ClubKitPlanType.POPULARITY => "按演练人气",
            ClubKitPlanType.KNOWLEDGE => "按演练科技",
            ClubKitPlanType.FIVE => "按五维总和",

            _ => "未设定",
        };
        public static string ClubKitPlanTypeAttrib(ClubKitPlanType type) => type switch
        {
            ClubKitPlanType.SONCOMBAT => "son_combat",
            ClubKitPlanType.LEADERSHIP => "leadership",
            ClubKitPlanType.COMBAT => "combat",

            ClubKitPlanType.MAX_HP => "max_hp",
            ClubKitPlanType.ATTACK => "attack",
            ClubKitPlanType.DEFENSE => "defense",
            ClubKitPlanType.COMBO => "combo",

            ClubKitPlanType.CHARISMA => "charisma",
            ClubKitPlanType.WIT => "wit",
            ClubKitPlanType.LUCK => "luck",
            ClubKitPlanType.POPULARITY => "popularity",
            ClubKitPlanType.KNOWLEDGE => "knowledge",
            ClubKitPlanType.FIVE => "five",

            _ => "",
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

        MAX_HP = 4,
        ATTACK = 5,
        DEFENSE = 6,
        COMBO = 7,

        CHARISMA = 8,
        WIT = 9,
        LUCK = 10,
        POPULARITY = 11,
        KNOWLEDGE = 12,
        FIVE = 13,
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
