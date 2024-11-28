using Newtonsoft.Json.Linq;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.test
{
    internal class WechatTest
    {
        public void Test(bool isRestart)
        {
            //        var wechat = new RS.Snail.JJJ.Wechat.Service(isRestart);
            //        bool v = wechat.Init(new List<string> { "ringoo" },
            //                            msgCallback: msg => Console.Write(msg), 
            //                            recallCallback: OnMessageRecall);
            //        wechat.StartReceive();
            //        // wechat.MsgStartImageHook("ringoo", "J:\\Projects\\RS\\Snail\\JJJ\\RS.JJJ\\bin\\Debug\\net6.0\\ReceivedImg");

            //        do
            //        {
            //            try
            //            {
            //                var code = Console.ReadLine();
            //                if (code == "self")
            //                {

            //                    var data = wechat.GetSelfInfo("ringoo") ?? new JObject();
            //                    Console.WriteLine(data);
            //                }
            //                else if (code == "contact")
            //                {
            //                    var data = wechat.ContactGetList("ringoo");
            //                    Console.WriteLine(data);
            //                }
            //                else if (code == "groupnick")
            //                {
            //                    var dic = wechat.ContaceChatGroupMemberNames("ringoo");
            //                    Console.WriteLine(dic);
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                Console.WriteLine(ex);
            //            }


            //        } while (true);
            //    }

            //    private void OnMessageReceived(dynamic msg)
            //    {
            //        Console.WriteLine(msg);
            //    }
            //    private void OnMessageRecall(dynamic msg)
            //    {
            //        Console.BackgroundColor = ConsoleColor.Red;
            //        Console.ForegroundColor = ConsoleColor.White;
            //        Console.WriteLine($"{JSONHelper.ParseString(msg.time)} {JSONHelper.ParseString(msg.sender)}撤回了消息：{JSONHelper.ParseString(msg.message)}");
            //        Console.ResetColor();
            //    }
        }
    }
}
