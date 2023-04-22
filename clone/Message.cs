using Google.Protobuf.WellKnownTypes;
using RS.Snail.JJJ.boot;
using RS.Tools.Common.Utils;
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
        public string Self { get; private set; }
        public long MsgID { get; private set; }
        public string FilePath { get; private set; }
        public string Sender { get; private set; }
        public string WXID { get; private set; }
        public long TimeStamp { get; private set; }
        public RS.Tools.Common.Enums.WechatMessageType Type { get; private set; }
        private robot.include.ChatScene _scene = robot.include.ChatScene.UNDEFINDE;
        public robot.include.ChatScene Scene
        {
            get
            {
                if (_scene != robot.include.ChatScene.UNDEFINDE) return _scene;
                if (Sender.EndsWith("@chatroom")) _scene = robot.include.ChatScene.Group;
                else _scene = robot.include.ChatScene.Private;
                return _scene;
            }
        }
        public string Extra { get; private set; }

        private string[]? _atWxids;
        /// <summary>
        /// at到的id
        /// </summary>
        public string[]? AtWxids
        {
            get
            {
                try
                {
                    if (_atWxids is not null) return _atWxids;
                    else if (string.IsNullOrWhiteSpace(Extra)) _atWxids = new string[0];
                    else if (!Extra.Contains("<atuserlist><![CDATA[")) _atWxids = new string[0];
                    else _atWxids = Extra.Split("<atuserlist><![CDATA[").Last().Split("]]></atuserlist>").First().Split(",", StringSplitOptions.RemoveEmptyEntries);
                    return _atWxids;
                }
                catch (Exception ex)
                {
                    Context.Logger.Write(ex, "AtWxids");
                    return null;
                }
            }
        }
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
                var arr = Content.Replace("\n", " ").Replace("\r", " ").Replace("@", " ").Replace(StringHelper.Chr(0x85), $" ").Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (arr.Length > 0)
                {
                    if (arr.FirstOrDefault().ToLower().StartsWith("jijiji"))
                    {
                        arr[0] = arr[0].Substring(6);
                    }
                    else
                    {
                        var firstPintyin = Pinyin.GetPinyin(arr.FirstOrDefault()).ToLower();
                        if (firstPintyin == "jijiji")
                        {
                            arr.Skip(1);
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
                if (_isCallingJijiji || (AtWxids is not null && AtWxids.Contains(Self))) return true;
                return Pinyin.GetPinyin(Content).ToLower().Contains("jijiji");
            }
        }

        public Message(dynamic data)
        {
            Content = JSONHelper.ParseString(data["message"]);
            Self = JSONHelper.ParseString(data["self"]);
            MsgID = JSONHelper.ParseLong(data["msgid"]);
            FilePath = JSONHelper.ParseString(data["filepath"]);
            Sender = JSONHelper.ParseString(data["sender"]);
            WXID = JSONHelper.ParseString(data["wxid"]);
            TimeStamp = JSONHelper.ParseLong(data["timestamp"]);
            Extra = JSONHelper.ParseString(data["extrainfo"]);
            Type = (RS.Tools.Common.Enums.WechatMessageType)JSONHelper.ParseInt(data["type"]);
        }

    }
}
