// See https://aka.ms/new-console-template for more information
using Aliyun.OSS.Util;
using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.robot.include;
using RS.Snail.JJJ.test;
using RS.Snail.JJJ.utils;
using RS.Tools.Common.Utils;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace RS.Snail.JJJ;
public class Entry
{
    // Main函数异步启动并且接收命令数组
    private static Context _context;
    static async Task Main(string[] args)
    {
        bool isRestart = false;
        bool isTest = false;
        if (args.Length > 0)
        {
            foreach (var item in args)
            {
                if (item == "restart") isRestart = true;
                if (item == "test") isTest = true;
            }
        }

        _context = new Context(isRestart, isTest);
        await Input();
    }

    // 等待并接收指令
    async private static Task Input()
    {
        await Task.Run(() =>
        {
            do
            {
                // Your code could perform some useful task in the following loop. However, 
                // for the sake of this example we'll merely pause for a quarter second.

                while (Console.KeyAvailable == false) Task.Delay(250).Wait(); // Loop until input is entered.
                var str = Console.ReadLine();
                if (string.IsNullOrEmpty(str)) continue;
                AcceptCommand(str);
            } while (true);
        });
    }

    private static void AcceptCommand(string command)
    {
        switch (command.ToLower())
        {
            case "exit":
            case "stop":
                _context.Exit();
                break;
            case "reboot":
            case "restart":
                _context.Restart();
                break;
            case "save":
                _context.BackupM.SaveNow();
                break;
            case "login":

                break;
            case "weekday":
                var wd = Snail.JJJ.Client.core.game.module.TimeM.GetWeekDayStatic();
                Console.WriteLine($"星期{wd}");
                break;
            default:
                Console.WriteLine("你说啥？");
                return;
        }
    }





    //public static void Main()
    //{
    //    Console.WriteLine("hello world");
    //    // q: 如何按指定位数取反?
    //    do
    //    {
    //        var input = Console.ReadLine();
    //        if (StringHelper.IsNumeric(input))
    //        {
    //            var data = DivinationHelper.GetDivination(Convert.ToInt32(input));
    //            Console.WriteLine($"字数 {data.Length}");
    //            Console.WriteLine(data);
    //            Console.WriteLine();
    //        }

    //    } while (true);

    //    //var data = System.IO.File.ReadAllText("J:\\Projects\\RS\\Snail\\JJJ\\RS.JJJ\\bin\\Debug\\net6.0\\OUT\\zhouyi64.json");
    //    //var jo = JObject.Parse(data);
    //    //IOHelper.SaveCSV(Tools.Common.Enums.CSVType.RobotData, jo, "zhouyi64");
    //    // 检查启动参数是否有restart

    //}






}
