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
    internal class GroupMember
    {
        public bool IsInGroup { get; set; }
        /// <summary>
        /// 更新时间
        /// </summary>
        public long UpdateTime { get; set; }
        /// <summary>
        /// 成员wxid
        /// </summary>
        public string WXID { get; set; }
        /// <summary>
        /// 成员群昵称
        /// </summary>
        public string NickName { get; set; }

        public UserRole Role { get; set; }
        public List<string> UIDs { get; set; } = new List<string> { };
        #region JSON
        public GroupMember(string wxid)
        {
            UpdateTime = TimeHelper.ToTimeStamp();
            IsInGroup = true;
            this.WXID = wxid;
        }
        public GroupMember(string wxid, string nickName)
        {
            UpdateTime = TimeHelper.ToTimeStamp();
            this.WXID = wxid;
            this.NickName = nickName;
            IsInGroup = true;
        }
        public GroupMember(dynamic src)
        {

            UpdateTime = JSONHelper.ParseLong(src["update_time"]);
            WXID = JSONHelper.ParseString(src["wxid"]);
            NickName = CryptoHelper.DecryptBase64(JSONHelper.ParseString(src["nick_name"]));
            Role = (UserRole)JSONHelper.ParseInt(src["role"]);
            IsInGroup = JSONHelper.ParseBool(src["is_in_group"]);
            UIDs = JSONHelper.ParseStringList(src["uids"]);
        }
        /// <summary>
        /// 获取群成员的json
        /// </summary>
        /// <returns></returns>
        public dynamic GetJO()
        {
            return JObject.FromObject(new
            {
                update_time = UpdateTime,
                wxid = WXID,
                nick_name = CryptoHelper.EncryptBase64(NickName),
                role = (int)Role,
                uids = UIDs,
                is_in_group = IsInGroup,
            });
        }
      
        #endregion
    }
}
