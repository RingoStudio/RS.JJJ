using RS.Snail.JJJ.utils;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.test
{
    internal class ImageTest
    {
        public static void Kit()
        {
            var now = TimeHelper.ToTimeStamp();
            var duration = $"{TimeHelper.SimpleTimeDescJustDate(RS.Snail.JJJ.Client.core.game.module.TimeM.GetGWWeekStartTimeStatic(now))} - " +
                           $"{TimeHelper.SimpleTimeDescJustDate(RS.Snail.JJJ.Client.core.game.module.TimeM.GetGWWeekEndTimeStatic(now))}";
            var path = GWImageHelper.GetClubKitPlanImage(
                 race: "1",
                 type: "按领导力",
                 duration: duration,
                 clubName: "冰法软件开发部",
                 names: new Dictionary<robot.include.Kit, List<string>>
                 {
                     { robot.include.Kit.GANSI, new List<string>{
                         "敢死一",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二",
                         "敢死二 [锁定]",
                         "敢死二 [锁定]",
                     } },
                      { robot.include.Kit.KANTAN, new List<string>{
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一",
                          "勘探一 [锁定]",
                     } },
                       { robot.include.Kit.KULI, new List<string>{
                           "苦力一",
                           "苦力一",
                           "苦力一",
                           "苦力一 [锁定]",
                     } },
                 },
                 locks: new()
                );
            Console.WriteLine(path);
        }
    }
}
