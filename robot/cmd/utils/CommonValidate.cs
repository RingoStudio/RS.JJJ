using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.cmd.utils
{
    internal class CommonValidate
    {
        /// <summary>
        /// 检查订阅有效
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="rid"></param>
        /// <returns></returns>
        public static bool CheckPurchase(Context context, Message msg, string rid)
        {
            var purchase = context.PurchaseM.CheckPurchase(rid, msg);
            if (!purchase.result)
            {
                if (!string.IsNullOrEmpty(purchase.desc))
                {
                    context.WechatM.SendAtText(purchase.desc, new List<string> { msg.Sender }, msg.RoomID);
                }
                return false;
            }

            return true;
        }

    }
}
