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
//Console.WriteLine("Hello, World!");
//var context = new RS.Snail.QCSDK.models.Context()
//{
//    Account = "12345678900",
//    Password = "password",
//    ChannelType = RS.Tools.Common.Enums.ChannelType.ANDROID,
//};

//dynamic jo = JObject.FromObject(new
//{
//    message = @"WEtWq4qiYg3M2ru/z58PR2GIrf2sy3qRYMbwmGjj9C6WJNUZwMQ214n7LJvFyn6PDX8Qd/SO65JXLZEDDf5w3SqH5xcT08/xbHISnOqQAw/PKN4MnaIUAtWxaBfQmIF7m25DqebV876K5kj0bWuZ3/7Q",
//    timestamp = "1677921228",
//});

//var result = RS.Snail.QCSDK.SDK.DecodeAES(context, jo.ToString());
//// var result = RS.Snail.QCSDK.tasks.Init.Do(context);

//Console.WriteLine(result);
// AutoScripts.FixConditions("J:\\Projects\\RS\\Snail\\JJJ\\Client\\core\\gane\\module\\condition");
public class Entry
{
    // Main函数异步启动并且接收命令数组
    private static Context _context;
    static async Task Main(string[] args)
    {
        //bool isRestart = false;
        //bool isTest = true;
        //if (args.Length > 0)
        //{
        //    foreach (var item in args)
        //    {
        //        if (item == "restart") isRestart = true;
        //        if (item == "test") isTest = true;
        //    }
        //}
        ////var tester = new test.WechatTest();
        ////tester.Test(isRestart);
        //_context = new Context(isRestart, isTest);

        //var input_promise = Input();
        //await input_promise;

        var test = new WechatTest();
        test.Test(false);
    }

    // 等待并接收指令
    async private static ValueTask Input()
    {
        await Task.Run(async () =>
        {
            do
            {
                // Your code could perform some useful task in the following loop. However, 
                // for the sake of this example we'll merely pause for a quarter second.

                while (Console.KeyAvailable == false)
                    Task.Delay(250).Wait(); // Loop until input is entered.
                var str = Console.ReadLine();
                if (string.IsNullOrEmpty(str)) continue;
                await AcceptCommand(str);
            } while (true);
        });
    }

    async private static ValueTask AcceptCommand(string command)
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
