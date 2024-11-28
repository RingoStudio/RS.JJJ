using Newtonsoft.Json.Linq;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.clone
{
    internal class Wechat
    {
        #region FIELDS
        public string WXID { get; private set; }
        public string Remark { get; private set; }
        public List<string> Admins { get; private set; }
        public string ManagerGroup { get; private set; }
        public List<string> VisitorGroups { get; private set; }
        public List<string> NoResponseGroups { get; private set; }
        public string OurName { get; private set; }
        public bool IsActived { get; set; }
       
        #endregion

        #region INIT
        public Wechat(dynamic data)
        {
            data = data ?? new JObject();
            WXID = JSONHelper.ParseString(data["wxid"]);
            Remark = JSONHelper.ParseString(data["remark"]);
            Admins = JSONHelper.ParseStringList(data["admins"]);
            ManagerGroup = JSONHelper.ParseString(data["manager_group_id"]);
            VisitorGroups = JSONHelper.ParseStringList(data["special_group_ids"]);
            NoResponseGroups = JSONHelper.ParseStringList(data["no_response_group_ids"]);
            OurName = JSONHelper.ParseString(data["our_name"]);
        }
        #endregion
    }
}
