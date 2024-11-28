using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.robot.modules;
using RS.Snail.JJJ.utils;
using RS.Tools.Common.Utils;
using RS.WechatFerry.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.boot
{
    internal class Context
    {
        #region FIELDS
        public static CommandManager Reflactor { get; private set; } = new CommandManager();
        public static RS.Tools.Common.Utils.Logger Logger { get; private set; } = new Tools.Common.Utils.Logger("JJJ");

        public bool IsRestart { get; private set; }
        public bool IsTest { get; private set; }
        #endregion

        #region INIT
        private void InitModules()
        {
            ConfigsM = new(this);
            ScheduleM = new(this);
            BackupM = new(this);
            ContactsM = new(this);
            ClubsM = new(this);
            CdM = new(this);
            CommunicateM = new(this);
            GroupWarEventsM = new(this);
            WechatM = new(this);
            PurchaseM = new(this);
            SnailsM = new(this);
            ConversationM = new(this);
            QuestionnaireM = new(this);
            QianM = new(this);
            MaydayM = new(this);
            HandbookM = new(this);

            Console.WriteLine("唧唧叽正在初始化");
            ConfigsM.Init();
            ShowInitDialog("初始化配置 ConfigsM");

            ScheduleM.Init();
            ShowInitDialog("初始化计时器 ScheduleM");

            BackupM.Init();
            ShowInitDialog("初始化备份管理 BackupM");

            ContactsM.Init();
            ShowInitDialog("初始化联系人管理 ContactsM");

            ClubsM.Init();
            ShowInitDialog("初始化俱乐部管理 ClubsM");

            CdM.Init();
            ShowInitDialog("初始化CD管理 CdM");

            CommunicateM.Init();
            ShowInitDialog("初始化消息管理 CommunicateM");

            GroupWarEventsM.Init();
            ShowInitDialog("初始化物种事件管理 GroupWarEventsM");

            WechatM.Init();
            ShowInitDialog("初始化微信接入 WechatM");

            PurchaseM.Init();
            ShowInitDialog("初始化订购管理 PurchaseM");

            SnailsM.Init();
            ShowInitDialog("初始化蜗牛登录管理 SnailsM");

            ConversationM.Init();
            ShowInitDialog("初始化对话管理 ConversationM");

            QuestionnaireM.Init();
            ShowInitDialog("初始化调查问卷 QuestionnaireM");

            QianM.Init();
            MaydayM.Init();
            HandbookM.Init();

            ShowInitDialog("唧唧叽初始化完成");
            Console.WriteLine("");
            Console.WriteLine("");

            Started();

            ScheduleM.StartExecute();
        }

        private void ShowInitDialog(string content)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            RS.Tools.Common.Utils.ConsoleLog.ClearCurrentConsoleLine();
            Console.Write(content);
        }

        public Context(bool isRestart, bool isTest)
        {
            IsRestart = isRestart;
            IsTest = isTest;
            InitModules();
            GC();
            RegistSchedule();

            if (IsTest) InitTest();
        }

        private void RegistSchedule()
        {
            // 重新启动
            ScheduleM.RegistSession("Context",
                                    times: new List<string>
                                    {
                                        "6-5-30-0",
                                        "7-5-30-0",
                                        "1-5-30-0",
                                        "2-5-30-0",
                                        "3-5-30-0",
                                        "4-5-30-0",
                                        "5-5-30-0",
                                    },
                                    action: new Action(() =>
                                    {
                                        Restart(30);
                                    }),
                                    isAutoLogin: false);

            ScheduleM.RegistSession("Context",
                                    3610 * 3,
                                    GC,
                                    false);
        }
        #endregion

        #region MODULES
        public ConfigsM ConfigsM { get; private set; }
        public ScheduleM ScheduleM { get; private set; }
        public BackupM BackupM { get; private set; }
        public ContactsM ContactsM { get; private set; }
        public ClubsM ClubsM { get; private set; }
        public CdM CdM { get; private set; }
        public CommunicateM CommunicateM { get; private set; }
        public WechatM WechatM { get; private set; }
        public PurchaseM PurchaseM { get; private set; }
        public SnailsM SnailsM { get; private set; }
        public ConversationM ConversationM { get; private set; }
        public QuestionnaireM QuestionnaireM { get; private set; }

        public GroupWarEventsM GroupWarEventsM { get; private set; }
        public QianM QianM { get; private set; }
        public MaydayM MaydayM { get; private set; }
        public HandbookM HandbookM { get; private set; }
        #endregion

        #region COMMUNICATE WECHAT

        #endregion

        #region SYSTEM
        public void GC()
        {
            System.GC.Collect();
            Console.WriteLine(">> 执行垃圾清理");
        }
        public void Started()
        {
            WechatM.SendMgrNotice($"你好，我是唧唧叽，很高兴为你服务！", atAdmins: true);
            WechatM.StartReceive();
        }
        /// <summary>
        /// 重新启动当前主程序
        /// </summary>
        public void Restart(int interval = 60)
        {
            Task.Run(() =>
            {
                WechatM.StopReceive();
                WechatM.ClearMessageQueue();
                WechatM.SendMgrNotice($"{robot.include.emoji.ZHUYI}唧唧叽将在{interval}秒后重启，待会见！", atAdmins: true);
                Thread.Sleep(interval * 1000);
                WechatM.StopWCF();
                BackupM.SaveNow();
                System.Diagnostics.Process.Start("RS.Snail.JJJ.exe", "restart");
                Console.WriteLine("bye");
                System.Environment.Exit(0);
            });
        }
        /// <summary>
        /// 退出唧唧叽
        /// </summary>
        public void Exit(int interval = 60)
        {
            Task.Run(() =>
            {
                WechatM.ClearMessageQueue();
                WechatM.StopReceive();
                WechatM.SendMgrNotice($"{robot.include.emoji.ZHUYI}唧唧叽将在{interval}秒后关闭，后会有期！", atAdmins: true);
                Thread.Sleep(interval * 1000);
                WechatM.StopWCF();
                BackupM.SaveNow();
                Console.WriteLine("bye");
                System.Environment.Exit(0);
            });
        }


        #endregion

        #region TEST
        private void InitTest()
        {
            do
            {
                var input = Console.ReadLine();

                switch (input)
                {
                    case "/clearcd":
                        CdM.ClearCache();
                        return;
                }

                var commaond = new List<string>();
                var wxid = "？？？";
                var atJJJ = false;
                var isGroup = false;
                var roomId = "";
                foreach (var item in input.Split(" "))
                {
                    if (item == "jjj") atJJJ = true;
                    else if (item == "g")
                    {
                        roomId = "？？？@chatroom";
                        isGroup = true;
                    }
                    else if (item == "p") wxid = "？？？";
                    else commaond.Add(item);
                }
                RecvMsg msg = new(isSelf: false,
                                  isGroup: isGroup,
                                  ID: (ulong)(new Random().NextInt64()),
                                  type: Tools.Common.Enums.WechatMessageType.Text,
                                  timeStamp: (ulong)TimeHelper.ToTimeStamp(),
                                  roomID: roomId,
                                  content: string.Join(" ", commaond),
                                  sender: wxid,
                                  sign: "",
                                  thumb: "",
                                  extra: "",
                                  xml: "");

                CommunicateM.ReceiveMessage(msg);



            } while (true);
        }
        #endregion
    }
}
