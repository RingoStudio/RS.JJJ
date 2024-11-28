using Google.Protobuf.WellKnownTypes;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Utils;
using RS.WechatFerry.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.clone
{
    internal class Message
    {
        public string Content { get; private set; }
        public ulong MsgID { get; private set; }
        public string Sender { get; private set; }
        public string RoomID { get; private set; }
        public ulong TimeStamp { get; private set; }
        public bool IsGroup { get; private set; }
        public string Self { get; private set; }

        public List<string> AtWxids { get; }
        public RS.Tools.Common.Enums.WechatMessageType Type { get; private set; }
        private robot.include.ChatScene _scene = robot.include.ChatScene.UNDEFINDE;
        public robot.include.ChatScene Scene
        {
            get => IsGroup ? robot.include.ChatScene.Group : _scene = robot.include.ChatScene.Private;
        }
        public string Extra { get; private set; }

        private string[]? _atWxids;
        /// <summary>
        /// at到的id
        /// </summary>
        private string[]? _explodeContent;
        private bool _isCallingJijiji = false;
        /// <summary>
        /// 炸开后的内容
        /// </summary>
        public string[] ExplodeContent
        {
            get
            {
                if (_explodeContent is not null) return _explodeContent;
                var arr = Content.Replace("\n", " ")
                    .Replace("\r", " ")
                    .Replace("@", " ")
                    .Replace(chars.AtSpliter, " ")
                    .Replace(((char)0x85).ToString(), " ")
                    .Trim()
                    .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (arr.Length > 0)
                {
                    if ((arr.FirstOrDefault() ?? "").ToLower().StartsWith("jijiji"))
                    {
                        arr[0] = arr[0].Substring(6);
                    }
                    else
                    {
                        var firstPintyin = Pinyin.GetPinyin(arr.FirstOrDefault()).ToLower();
                        if (firstPintyin == "jijiji")
                        {
                            arr = arr.Skip(1).ToArray();
                            _isCallingJijiji = true;
                        }
                        else if (firstPintyin.StartsWith("jijiji"))
                        {
                            arr[0] = arr[0].Substring(3);
                            _isCallingJijiji = true;
                        }
                    }
                }
                _explodeContent = arr;
                return _explodeContent;
            }
        }
        /// <summary>
        /// 是否在呼叫唧唧叽
        /// </summary>
        public bool IsCallingJijiji
        {
            get
            {
                var arr = ExplodeContent;
                //if (_isCallingJijiji) return true;
                //foreach (var item in arr)
                //{
                //    if (Pinyin.GetPinyin(arr.FirstOrDefault()).ToLower() == "jijiji") return true;
                //}
                //return false;
               // if (IsAtAll) return false;
                if (_isCallingJijiji || (AtWxids is not null && AtWxids.Contains(Self))) return true;
                return Pinyin.GetPinyin(Content).ToLower().Contains("jijiji");
            }
        }

        public bool IsAtAll
        {
            get => (Content ?? "").Contains($"@所有人{robot.include.chars.AtSpliter}")|| (Content ?? "").Contains($"@All{robot.include.chars.AtSpliter}");
        }

        public Message(dynamic data)
        {
            Content = JSONHelper.ParseString(data["message"]);
            MsgID = JSONHelper.ParseLong(data["msgid"]);
            Sender = JSONHelper.ParseString(data["sender"]);
            TimeStamp = JSONHelper.ParseLong(data["timestamp"]);
            Extra = JSONHelper.ParseString(data["extrainfo"]);
            Type = (RS.Tools.Common.Enums.WechatMessageType)JSONHelper.ParseInt(data["type"]);
        }

        public Message(RecvMsg msg, string self)
        {
            Content = msg.Content;
            MsgID = msg.ID;
            Sender = msg.Sender;
            TimeStamp = msg.TimeStamp;
            Extra = msg.Extra;
            Type = msg.MessageType;
            AtWxids = msg.AtWxids;
            RoomID = msg.RoomID;
            IsGroup = msg.IsGroup;
            Self = self;
        }

    }
}
