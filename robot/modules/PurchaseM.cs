using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.modules
{
    /// <summary>
    /// 叽叽叽订购管理
    /// </summary>
    internal class PurchaseM : IModule
    {
        #region FIELDS
        public string ModuleName { get => "BackupM"; }
        public Context _context { get; set; }

        private bool _inited = false;
        public bool Inited { get => _inited; }

        private List<string> _backupSessions;
        private Dictionary<string, Action> _saveSessions;
        #endregion

        #region INIT
        public PurchaseM(boot.Context context)
        {
            this._context = context;
        }
        public void Init(bool load = false)
        {
            if (_inited) return;
            _inited = true;
            //_backupSessions = new();
            //RegistSchedule();
        }
        #endregion

        #region PUBLIC METHODS

        // 订阅提醒 只对经理及以上有效
        // 1. 临近订阅期三天，每8小时提醒一次
        // 2. 临近订阅期一天，每4小时提醒一次
        // 3. 超时一天，每2小时提醒一次
        // 4. 超时三天，每4小时提醒一次
        // 5. 超时多天，每8小时提醒一次

        // 订阅自动提醒
        // 每天中午十二点检查一次临近24小时超期以及已超期

        // 订阅查询
        // 生成excel
        // RID, NAME, CHANNEL, HOLDER, CLUB_NAME, GROUP_NAME, START_PURCHASE, END_PURCHASE



        /// <summary>
        /// 检查订阅期并提醒
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>true - 未过期，false - 已过期</returns>
        public (bool result, string? desc) CheckPurchase(string rid, Message msg)
        {
            // scene    role    note        
            // group    holder  提醒      
            // group    mgr     提醒      
            // group    member  

            var role = _context.ContactsM.QueryRole(msg.Sender, msg.RoomID);
            if (role == include.UserRole.ADMINISTRATOR) return (true, null);


            var club = _context.ClubsM.FindClub(rid);
            if (club is null) return (false, null);
            if (club.PurchaseEnd == long.MinValue) return (true, null);

            var purchaseEnd = club.PurchaseEnd;
            var now = TimeHelper.ToTimeStamp();
            if (role <= include.UserRole.NORMAL)
            {
                if (now > purchaseEnd) return (false, null);
                else return (true, null);
            }

            var lastNotice = _context.CdM.GetCache($"purchase_notice_{rid}");
            var delta = now - purchaseEnd;
            if (delta < -86400 * 3)
            {
                return (true, null);
            }
            else if (delta < -86400 * 2)
            {
                //  临近订阅期三天，每8小时提醒一次
                string? desc = now - lastNotice > 8 * 3600 ? "您所在俱乐部的唧唧叽订阅将在3天内到期。" : null;
                return (true, desc);
            }
            else if (delta < -86400 * 1)
            {
                //  临近订阅期二天，每8小时提醒一次
                string? desc = now - lastNotice > 8 * 3600 ? "您所在俱乐部的唧唧叽订阅将在2天内到期。" : null;
                return (true, desc);
            }
            else if (delta < 0)
            {
                //  临近订阅期一天，每4小时提醒一次
                string? desc = now - lastNotice > 4 * 3600 ? "您所在俱乐部的唧唧叽订阅将在1天内到期。" : null;
                return (true, desc);
            }
            else if (delta > 3 * 86400)
            {
                string? desc = now - lastNotice > 8 * 3600 ? $"您所在俱乐部的唧唧叽订阅已超期 {delta / 86400} 天。\n" +
                                                             $"请及时续订。" : null;
                return (false, desc);
            }
            else if (delta > 1 * 86400)
            {
                string? desc = now - lastNotice > 4 * 3600 ? $"您所在俱乐部的唧唧叽订阅已超期 {delta / 86400} 天。\n" +
                                                             $"请及时续订。" : null;
                return (false, desc);
            }
            else
            {
                string? desc = now - lastNotice > 2 * 3600 ? $"您所在俱乐部的唧唧叽订阅今天已经到期。\n" +
                                                             $"请及时续订。" : null;
                return (false, desc);
            }

        }
        #endregion

    }
}
