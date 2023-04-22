using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.clone
{
    internal class User
    {
        public long UpdateTime { get; set; }
        public string ID { get; set; }
        public string WXID { get; set; }
        public string Node { get; set; }
        public string Alias { get; set; }
        public string NickName { get; set; }
        public string Remark { get; set; }
        public string BigHeadImgUrl { get; set; }
        /// <summary>
        /// 作为会长的俱乐部RID
        /// </summary>
        public List<string> HoldsRIDs { get; set; } = new List<string> { };
        public UserRole Role { get; set; }
        public bool IsFriend { get; set; }

        #region 
        //public long last_qian_time { get; set; }
        //private int _qian_today;
        ///// <summary>
        ///// 抽签状态 0-今日未抽未解，1-今日已抽未解，2-今日已抽已解
        ///// </summary>
        //public int qian_today
        //{
        //    get
        //    {
        //        if (!RS_SNAIL_DELEGATE.game.module.TimeM.Instance().IsSameDay(last_qian_time))
        //            _qian_today = 0;
        //        return _qian_today;
        //    }
        //    set
        //    {
        //        _qian_today = value;
        //        last_qian_time = Utils.TimeHelper.ToTimeStamp();
        //    }
        //}
        #endregion

        #region JSON
        public User(string wxid, string nick)
        {
            UpdateTime = TimeHelper.ToTimeStamp();
            this.WXID = wxid;
            this.NickName = nick;
        }

        /// <summary>
        /// 刷新时新建对象
        /// </summary>
        /// <param name="src"></param>
        /// <param name="wxid"></param>
        public User(dynamic src, string wxid)
        {
            UpdateTime = TimeHelper.ToTimeStamp();
            NickName = JSONHelper.ParseString(src["name"]);
            Node = JSONHelper.ParseString(src["node"]);
            ID = JSONHelper.ParseString(src["wxcode"]);
            this.WXID = wxid;
        }
        /// <summary>
        /// 从csv导入时新建对象
        /// </summary>
        /// <param name="src"></param>
        public User(dynamic src)
        {
            UpdateTime = JSONHelper.ParseLong(src["update_time"]);
            ID = JSONHelper.ParseString(src["id"]);
            WXID = JSONHelper.ParseString(src["wxid"]);
            Node = JSONHelper.ParseString(src["node"]);
            Alias = JSONHelper.ParseString(src["alias"]);
            NickName = CryptoHelper.DecryptBase64(JSONHelper.ParseString(src["nick_name"]));
            Remark = CryptoHelper.DecryptBase64(JSONHelper.ParseString(src["remark"]));
            BigHeadImgUrl = JSONHelper.ParseString(src["big_head_img_url"]);

            //last_qian_time = JSONHelper.ParseJTokenToLng(src, "last_qian_time");
            //_qian_today = JSONHelper.ParseJTokenToInt(src, "qian_today");

            HoldsRIDs = JSONHelper.ParseStringList(src["rids"]);
            Role = (UserRole)JSONHelper.ParseInt(src["role"]);
            IsFriend = JSONHelper.ParseBool(src["is_friend"]);
        }
        public void RefreshInfo(dynamic src)
        {
            var tmp = JSONHelper.ParseString(src["wxid"]);
            if (tmp.Contains("@chatroom")) return;
            WXID = tmp;
            NickName = JSONHelper.ParseString(src["name"]);
            UpdateTime = TimeHelper.ToTimeStamp();
        }
        public dynamic GetJO()
        {
            return JObject.FromObject(new
            {
                update_time = UpdateTime,
                id = ID,
                wxid = WXID,
                node = Node,
                alias = Alias,
                nick_name = CryptoHelper.EncryptBase64(NickName),
                remark = CryptoHelper.EncryptBase64(Remark),
                big_head_img_url = BigHeadImgUrl,
                rids = HoldsRIDs,
                role = (int)Role,
                is_friend = IsFriend,
            });
        }
        #endregion
        #region OPERATOR
        public override bool Equals(dynamic obj)
        {
            if (obj is User)
            {
                var user = obj as User;
                return user?.WXID == this.WXID;
            }
            else if (obj is GroupMember)
            {
                var gm = obj as GroupMember;
                return gm?.WXID == this.WXID;
            }
            return false;
        }
        #endregion
    }
}
