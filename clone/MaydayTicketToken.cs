using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.clone
{
    internal class MaydayTicketToken
    {
        public MaydayTicketToken(Message msg)
        {
            this.Wxid = msg.Sender;
            this.Chatroom = msg.RoomID;
            RequestTime = TimeHelper.ToTimeStamp();
        }
        public string Chatroom { get; set; }
        public string Wxid { get; set; }
        public long RequestTime { get; set; }
        public const string MarkBirthDay = "生日";
        public string BirthDay { get; set; }
        public bool Stat { get; set; }
        public const string MarkDate = "日期";
        public string Date { get; set; }
        public const string MarkGate = "入口";
        public string Gate { get; set; }
        public const string MarkArea = "区域";
        public string Area { get; set; }
        public const string MarkPos = "座位";
        public string Pos { get; set; }
        public const string MarkPrice = "价格";
        public string Price { get; set; }
        public const string MarkAudienceName = "观演人";
        public string AudienceName { get; set; }
        public const string MarkNo = "序号";
        public string No { get; set; }
        public const string MarkTicketNo = "票号";
        public string TicketNo { get; set; }
        public const string MarkIsBleed = "出血";
        public bool IsBleed { get; set; } = false;
        public const string MarkIsCompress = "压缩";
        public bool IsCompress { get; set; } = true;

        public static string GetID(Message msg) => $"{msg.Self}_{msg.Sender}_{msg.RoomID}";
    }
}
