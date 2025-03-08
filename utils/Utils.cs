using RS.Tools.Common.Utils;
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
        /// <summary>
        /// 将给定的文件拷贝到USER/CACHE目录中，若重名，自动用序号顺延防止覆盖
        /// </summary>
        /// <param name="oriPath"></param>
        /// <returns></returns>
        public static string SaveReceivedFile(string oriPath)
        {
            if (string.IsNullOrEmpty(oriPath) || !System.IO.File.Exists(oriPath)) return oriPath;
            var fileName = IOHelper.GetFileNameWithoutExtension(oriPath);
            var extensionName = IOHelper.GetExtension(oriPath);
            var targetPath = "";
            int index = 0;
            do
            {
                targetPath = index == 0 ? Path.Join("USER\\CACHE", $"{fileName}.{extensionName}") : Path.Join("USER\\CACHE", $"{fileName}({index}).{extensionName}");
                if (!System.IO.File.Exists(targetPath)) break;
                index++;
            } while (true);
            System.IO.File.Copy(oriPath, targetPath, true);
            return targetPath;
        }
    }
}
