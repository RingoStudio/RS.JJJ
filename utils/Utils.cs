using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.utils
{
    internal class Utils
    {
        private static List<string> _reservedNames = new List<string>
        {
            "CON",
            "PRN",
            "AUX",
            "NUL",
            "COM1",
            "COM2",
            "COM3",
            "COM4",
            "COM5",
            "COM6",
            "COM7",
            "COM8",
            "COM9",
            "LPT1",
            "LPT2",
            "LPT3",
            "LPT4",
            "LPT5",
            "LPT6",
            "LPT7",
            "LPT8",
            "LPT9"
        };
        public static string GetNickNameForFileName(string nick)
        {
            foreach (var c in "<>:\"/\\|?*")
            {
                nick = nick.Replace(c.ToString(), "");
            }

            foreach (var item in _reservedNames)
            {
                if (nick.ToUpper() == item.ToUpper()) return "";
            }

            return nick;
        }
    }
}
