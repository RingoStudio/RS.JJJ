using RS.Snail.QCSDK.misc;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace RS.Snail.JJJ.utils
{
    internal class FontHelper
    {
        private static bool _inited = false;
        private static System.Drawing.Text.PrivateFontCollection _fonts = null;
        private static System.Drawing.Text.PrivateFontCollection _fonts2 = null;
        private static string _defaultFontName = null;

        private static string _fontFilePath = @"RES\FONTS\{0}.ttf";
        private static string _fontResourcePath = @"pack://application:,,,/RS.Snail.SSE.WPF;component/ui/res/font/{0}.ttf";

        private static List<string> _fontNames = new List<string>
        {
            "HarmonyOS_Sans_SC_Black",
            "HarmonyOS_Sans_SC_Bold",
            "HarmonyOS_Sans_SC_Light",
            "HarmonyOS_Sans_SC_Medium",
            "HarmonyOS_Sans_SC_Regular",
        };
        /// <summary>
        /// 初始化字体
        /// </summary>
        private static void Init()
        {
            if (_inited && _fonts is not null) return;

            _fonts = new System.Drawing.Text.PrivateFontCollection();
            _fonts2 = new System.Drawing.Text.PrivateFontCollection();

            foreach (var _name in _fontNames)
            {
                try
                {
                    var targetPath = string.Format(_fontFilePath, _name);
                    if (_name == _fontNames.Last()) _fonts2.AddFontFile(targetPath);
                    else _fonts.AddFontFile(targetPath);

                }
                catch (Exception ex)
                {
                    Logger.Instance.WriteException(ex, "FontHelper.Init");
                }

            }
            _inited = true;
        }
        /// <summary>
        /// 获取系统默认字体
        /// </summary>
        /// <returns></returns>
        private static string GetSystemDefaultFontName()
        {
            if (!string.IsNullOrEmpty(_defaultFontName)) return _defaultFontName;

            _defaultFontName = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE/SOFTWARE/Microsoft/Windows NT/CurrentVersion/GRE_Initialize", "GUIFont.Facename", "SimSun") as string ?? "SimSun";

            return _defaultFontName;
        }
        /// <summary>
        /// 获取指定Winform字体
        /// </summary>
        /// <param name="weight">字重</param>
        /// <param name="size">字号（磅）</param>
        /// <returns></returns>
        public static System.Drawing.Font GetFont(FontWeight weight, double size)
        {

            Init();

            if (size <= 0) size = 10;
            // size /= ScreenHelper.GetScalingRatio();

            try
            {
                if (weight == FontWeight.Black) return new System.Drawing.Font(_fonts.Families[0], Convert.ToSingle(size));
                else if (weight == FontWeight.Bold) return new System.Drawing.Font(_fonts.Families[1], Convert.ToSingle(size));
                else if (weight == FontWeight.Light) return new System.Drawing.Font(_fonts.Families[2], Convert.ToSingle(size));
                else if (weight == FontWeight.Medium) return new System.Drawing.Font(_fonts.Families[3], Convert.ToSingle(size));
                else return new System.Drawing.Font(_fonts2.Families[0], Convert.ToSingle(size));
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteException(ex, "FontHelper.GetFontItem");
                return new System.Drawing.Font(GetSystemDefaultFontName(), Convert.ToSingle(size));
            }
        }

        public enum FontWeight
        {
            Regular = 1,
            Medium = 2,
            Light = 3,
            Bold = 4,
            Black = 5,
        }
    }
}
