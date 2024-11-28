using RS.Snail.JJJ.clone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.broadcast
{
    internal class BroadCast
    {
        public string ID { get => $"{Self}_{Sender}_{RoomID}"; }
        public string Key;
        public string RID;
        public string Self;
        public string Sender;
        private string RoomID;
        public List<string> Content = new List<string>();
        public List<string> Files = new List<string>();
        public List<string> Images = new List<string>();
        public int ContentLength
        {
            get
            {
                if (Content.Count == 0) return 0;
                int ret = 0;
                Content.Select(a => ret += a.Length);
                return ret;
            }
        }
        public int AttachCount { get => Files.Count + Images.Count; }
        public BroadCast(Message msg)
        {
            this.Sender = msg.Sender;
            this.Self = msg.Self;
            this.RoomID = msg.RoomID;
        }

        public static string GetID(Message msg)
        {
            return $"{msg.Self}_{msg.Sender}_{msg.RoomID}";
        }
    }
}
