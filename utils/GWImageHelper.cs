using Aliyun.OSS.Util;
using Google.Protobuf.WellKnownTypes;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.utils
{
    internal class GWImageHelper
    {
        private const string TAG = "ImageHelper";
        private static Color _colorDarkGray = Color.FromArgb(35, 24, 31);
        private static float _fontWidthRatio = 1.4F;
        private static object _locker = new object();
        private static object _saveLocker = new object();

        /// <summary>
        /// 从文件读取PNG
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static Bitmap LoadImage(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var path = AppDomain.CurrentDomain.BaseDirectory + @$"RES\IMG\jjj\{name}.png";
            lock (_locker)
            {
                if (!File.Exists(path)) return null;
                try
                {
                    return new Bitmap(path);
                }
                catch (Exception ex)
                {
                    Logger.Instance.WriteException(ex, TAG);
                    return null;
                }
            }
        }

        public static string FileName(string name, long time = -1)
        {
            if (time == -1) time = TimeHelper.ToTimeStamp();
            var timeStr = TimeHelper.ToDateTime(time).ToString("yyyy_MM_dd_HH_mm_ss");
            return $"OUT\\{name}_{timeStr}.png";
        }

        public static string SaveImage(Bitmap image, string name)
        {
            if (image == null) return null;
            var path = FileName(name);
            if (File.Exists(path)) return path;
            lock (_saveLocker)
            {
                try
                {
                    image.Save(path, ImageFormat.Png);
                    return path;
                }
                catch (Exception ex)
                {
                    Logger.Instance.WriteException(ex, TAG);
                    return "";
                }
            }
        }

        #region KIT
        /// <summary>
        /// 布阵图
        /// </summary>
        /// <param name="race"></param>
        /// <param name="duration"></param>
        /// <param name="clubName"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        internal static string? GetClubKitPlanImage(string race,
                                                    string type,
                                                    string duration,
                                                    string clubName,
                                                    Dictionary<Kit, List<string>> names,
                                                    List<string> locks)
        {
            try
            {
                var ICON = LoadImage("gw_kit");
                if (ICON == null) return null;
                var b = new Bitmap(1255 * 2, 1310 * 2, PixelFormat.Format32bppArgb);
                var g = Graphics.FromImage(b);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(Color.Transparent);
                g.DrawImage(ICON,
                    new Rectangle(0, 0, b.Width, b.Height),
                    new Rectangle(0, 0, ICON.Width, ICON.Height),
                    GraphicsUnit.Pixel);

                Point pos = new Point();

                #region CLUB NAME
                (pos.X, pos.Y) = (70, 300);
                DrawStringAct(ref g,
                  clubName,
                  FontHelper.GetFont(FontHelper.FontWeight.Black, 120),
                  Color.White,
                  pos,
                  StringAlignment.Near);
                #endregion

                #region PERIOD & RACE
                (pos.X, pos.Y) = (2468, 245);
                DrawStringAct(ref g,
                  duration,
                  FontHelper.GetFont(FontHelper.FontWeight.Bold, 36),
                  Color.White,
                  pos,
                  StringAlignment.Far);

                var desc = race switch
                {
                    "1" => "蜣螂图",
                    "2" => "金鱼图",
                    "3" => "北极贝图",
                    "4" => "仓鼠图",
                    "5" => "螳螂图",
                    _ => "未知种族",
                };

                desc = $"[{type}] {desc}";

                (pos.X, pos.Y) = (2448, 330);
                DrawStringAct(ref g,
                 desc,
                 FontHelper.GetFont(FontHelper.FontWeight.Bold, 36),
                 Color.White,
                 pos,
                 StringAlignment.Far);
                #endregion
                #region NAME LIST
                int kit_1_x = 495;
                int kit_2_x = 1295;
                int kit_3_x = 2095;
                int kit_1_y = 595;
                int kit_2_y = 595;
                int kit_3_y = 595;
                Color color;
                foreach (var item in names)
                {
                    foreach (var name in item.Value)
                    {

                        switch (item.Key)
                        {
                            case Kit.GANSI:
                                (pos.X, pos.Y) = (kit_1_x, kit_1_y);
                                kit_1_y += 80;
                                break;
                            case Kit.KANTAN:
                                (pos.X, pos.Y) = (kit_2_x, kit_2_y);
                                kit_2_y += 80;
                                break;
                            case Kit.KULI:
                                (pos.X, pos.Y) = (kit_3_x, kit_3_y);
                                kit_3_y += 80;
                                break;
                            default:
                                continue;
                        }
                        color = locks.Contains(name) ? Color.DarkBlue : Color.Black;
                        DrawStringAct(ref g,
                                     name,
                                     FontHelper.GetFont(FontHelper.FontWeight.Bold, 30),
                                     color,
                                     pos,
                                     StringAlignment.Center);
                    }
                }
                #endregion

                return SaveImage(b, $"布阵图_{clubName}");
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteException(ex, "ImageHelper.GetGWSuitImage");
                return null;
            }
        }
        /// <summary>
        /// 矿点表
        /// </summary>
        /// <param name="race"></param>
        /// <param name="duration"></param>
        /// <param name="clubName"></param>
        /// <param name="clubName"></param>
        /// <param name="distSort"></param>
        /// <param name="poses">区域, X行X列X</param>
        /// <returns></returns>
        internal static string? GetEventMineImage(string duration,
                                                  string clubName,
                                                  string distSort,
                                                  Dictionary<int, List<string>> poses)
        {
            try
            {
                var ICON = LoadImage("gw_event_mine_list");
                if (ICON == null) return null;

                var b = new Bitmap(1920, 2500, PixelFormat.Format32bppArgb);
                var g = Graphics.FromImage(b);

                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.Clear(Color.Transparent);
                g.DrawImage(ICON,
                    new Rectangle(0, 0, b.Width, b.Height),
                    new RectangleF(0, 0, ICON.Width, ICON.Height),
                    GraphicsUnit.Pixel);

                Point pos = new Point();
                var desc = $"[矿点表]  {clubName} [{distSort}]  {duration}";
                pos.X = 50;
                pos.Y = 68;
                DrawStringAct(ref g,
                    desc,
                    FontHelper.GetFont(FontHelper.FontWeight.Bold, 30),
                    _colorDarkGray,
                    pos,
                    StringAlignment.Near);

                foreach (var areaItem in poses)
                {
                    var area = areaItem.Key;
                    pos.X = 210;
                    pos.Y = 285 * 2 + 40 + (area - 1) * 80;
                    foreach (var item in areaItem.Value)
                    {
                        DrawStringAct(ref g,
                                     item,
                                     FontHelper.GetFont(FontHelper.FontWeight.Bold, 20),
                                     _colorDarkGray,
                                     pos,
                                     StringAlignment.Center);
                        pos.X += 200;
                    }
                }

                return SaveImage(b, $"矿点表_{clubName}");
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteException(ex, "ImageHelper.GetEventMineImage");
                return null;
            }
        }
        /// <summary>
        /// 生成事件表
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="clubName"></param>
        /// <param name="distSort"></param>
        /// <param name="poses"></param>
        /// <param name="singles"></param>
        /// <returns></returns>
        internal static string? GetEventMultiImage(string duration,
                                                string clubName,
                                                string distSort,
                                                Dictionary<int, List<string>> poses,
                                                List<string> singles)
        {
            try
            {
                var ICON = LoadImage("gw_event_multi_list");
                if (ICON == null) return null;

                var b = new Bitmap(1920, 2660, PixelFormat.Format32bppArgb);
                var g = Graphics.FromImage(b);

                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.Clear(Color.Transparent);
                g.DrawImage(ICON,
                    new Rectangle(0, 0, b.Width, b.Height),
                    new RectangleF(0, 0, ICON.Width, ICON.Height),
                    GraphicsUnit.Pixel);

                Point pos = new Point();
                var desc = $"[事件表]  {clubName} [{distSort}]  {duration}";
                pos.X = 50;
                pos.Y = 68;
                DrawStringAct(ref g,
                    desc,
                    FontHelper.GetFont(FontHelper.FontWeight.Bold, 30),
                    _colorDarkGray,
                    pos,
                    StringAlignment.Near);

                foreach (var areaItem in poses)
                {
                    var area = areaItem.Key;
                    pos.X = 410;
                    pos.Y = 285 * 2 + 40 + (area - 1) * 80;
                    foreach (var item in areaItem.Value)
                    {
                        DrawStringAct(ref g,
                                     item,
                                     FontHelper.GetFont(FontHelper.FontWeight.Bold, 30),
                                     _colorDarkGray,
                                     pos,
                                     StringAlignment.Center);
                        pos.X += 600;
                    }
                }
                if (singles is not null && singles.Count > 0)
                {
                    desc = string.Join(" | ", singles);
                    pos.X = 960;
                    pos.Y = 2610;
                    DrawStringAct(ref g,
                                    desc,
                                    FontHelper.GetFont(FontHelper.FontWeight.Bold, 30),
                                    _colorDarkGray,
                                    pos,
                                    StringAlignment.Center);
                }

                return SaveImage(b, $"事件表_{clubName}");
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteException(ex, "ImageHelper.GetEventMultiImage");
                return null;
            }
        }

        /// <summary>
        /// 生成boss表
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="clubName"></param>
        /// <param name="distSort"></param>
        /// <param name="monster"></param>
        /// <param name="boss"></param>
        /// <returns></returns>
        internal static string? GetEventBossImage(string duration,
                                                string clubName,
                                                string distSort,
                                                Dictionary<int, List<string>> monster,
                                                Dictionary<int, string> boss)
        {
            try
            {
                var ICON = LoadImage("gw_event_boss_list");
                if (ICON == null) return null;

                var b = new Bitmap(1920, 2500, PixelFormat.Format32bppArgb);
                var g = Graphics.FromImage(b);

                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.Clear(Color.Transparent);
                g.DrawImage(ICON,
                    new Rectangle(0, 0, b.Width, b.Height),
                    new RectangleF(0, 0, ICON.Width, ICON.Height),
                    GraphicsUnit.Pixel);

                Point pos = new Point();
                var desc = $"[BOSS表]  {clubName} [{distSort}]  {duration}";
                pos.X = 50;
                pos.Y = 68;
                DrawStringAct(ref g,
                    desc,
                    FontHelper.GetFont(FontHelper.FontWeight.Bold, 30),
                    _colorDarkGray,
                    pos,
                    StringAlignment.Near);

                foreach (var areaItem in monster)
                {
                    var area = areaItem.Key;
                    pos.X = 310;
                    pos.Y = 285 * 2 + 40 + (area - 1) * 80;
                    foreach (var item in areaItem.Value)
                    {
                        DrawStringAct(ref g,
                                     item,
                                     FontHelper.GetFont(FontHelper.FontWeight.Bold, 30),
                                     _colorDarkGray,
                                     pos,
                                     StringAlignment.Center);
                        pos.X += 400;
                    }
                }

                foreach (var areaItem in boss)
                {
                    var area = areaItem.Key;
                    pos.X = 1610;
                    pos.Y = 285 * 2 + 40 + (area - 1) * 80;
                    DrawStringAct(ref g,
                                  areaItem.Value,
                                  FontHelper.GetFont(FontHelper.FontWeight.Bold, 30),
                                  _colorDarkGray,
                                  pos,
                                  StringAlignment.Center);
                }

                return SaveImage(b, $"BOSS表_{clubName}");
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteException(ex, "ImageHelper.GetEventBossImage");
                return null;
            }
        }
        /// <summary>
        /// 生成boss表
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="clubName"></param>
        /// <param name="distSort"></param>
        /// <param name="monster"></param>
        /// <param name="boss"></param>
        /// <returns></returns>
        internal static string? GetEvenSumImage(string duration,
                                                string clubName,
                                                string distSort,
                                                Dictionary<int, string> multis,
                                                Dictionary<int, string> mines,
                                                Dictionary<int, string> monster,
                                                string boss,
                                                string singles)
        {
            try
            {
                var ICON = LoadImage("gw_event_sum_list");
                if (ICON == null) return null;

                var b = new Bitmap(1920, 2660, PixelFormat.Format32bppArgb);
                var g = Graphics.FromImage(b);

                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.Clear(Color.Transparent);
                g.DrawImage(ICON,
                    new Rectangle(0, 0, b.Width, b.Height),
                    new RectangleF(0, 0, ICON.Width, ICON.Height),
                    GraphicsUnit.Pixel);

                Point pos = new Point();
                var desc = $"[事件总表]  {clubName} [{distSort}]  {duration}";
                pos.X = 50;
                pos.Y = 68;
                DrawStringAct(ref g,
                    desc,
                    FontHelper.GetFont(FontHelper.FontWeight.Bold, 30),
                    _colorDarkGray,
                    pos,
                    StringAlignment.Near);

                #region MULTIS
                foreach (var areaItem in multis)
                {
                    var area = areaItem.Key;
                    pos.X = 410;
                    pos.Y = 285 * 2 + 40 + (area - 1) * 80;
                    foreach (var item in areaItem.Value)
                    {
                        DrawStringAct(ref g,
                                     areaItem.Value,
                                     FontHelper.GetFont(FontHelper.FontWeight.Bold, 30),
                                     _colorDarkGray,
                                     pos,
                                     StringAlignment.Center);
                        pos.X += 400;
                    }
                }
                #endregion

                #region MINES
                foreach (var areaItem in mines)
                {
                    var area = areaItem.Key;
                    if (area < 18) continue;
                    pos.X = 1340;
                    pos.Y = 285 * 2 + 120 + (area - 18) * 160;
                    DrawStringAct(ref g,
                                 areaItem.Value,
                                 FontHelper.GetFont(FontHelper.FontWeight.Bold, 25),
                                 _colorDarkGray,
                                 pos,
                                 StringAlignment.Center);

                }
                #endregion

                #region MONSTERS
                foreach (var areaItem in monster)
                {
                    var area = areaItem.Key;
                    if (area < 17) continue;
                    pos.X = 1340;
                    pos.Y = 285 * 2 + 120 + (area - 3) * 80;
                    DrawStringAct(ref g,
                                 areaItem.Value,
                                 FontHelper.GetFont(FontHelper.FontWeight.Bold, 25),
                                 _colorDarkGray,
                                 pos,
                                 StringAlignment.Center);
                }
                #endregion

                #region BOSS
                if (!string.IsNullOrEmpty(boss))
                {
                    pos.X = 1320;
                    pos.Y = 2450;
                    DrawStringAct(ref g,
                                 boss,
                                 FontHelper.GetFont(FontHelper.FontWeight.Bold, 30),
                                 _colorDarkGray,
                                 pos,
                                 StringAlignment.Center);
                }
                #endregion

                #region SINGLES
                if (!string.IsNullOrEmpty(singles))
                {
                    pos.X = 960;
                    pos.Y = 2610;
                    DrawStringAct(ref g,
                                 singles,
                                 FontHelper.GetFont(FontHelper.FontWeight.Bold, 30),
                                 _colorDarkGray,
                                 pos,
                                 StringAlignment.Center);
                }
                #endregion

                return SaveImage(b, $"事件总表_{clubName}");
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteException(ex, "ImageHelper.GetEvenSumImage");
                return null;
            }
        }


        #endregion

        #region UITLS
        private static void DrawStringAct(ref Graphics g,
          string str,
          Font font,
          Color color,
          Point point,
          StringAlignment datum = StringAlignment.Center)
        {
            DrawStringAct(ref g,
               str,
               font,
               new SolidBrush(color),
               point,
               datum);
        }
        private static void DrawStringAct(ref Graphics g,
            string str,
            Font font,
            Brush brush,
            Point point,
            StringAlignment datum = StringAlignment.Center)
        {
            var pointF = new PointF(point.X, point.Y);
            var size = g.MeasureString(str,
                font,
                pointF,
                StringFormat.GenericTypographic);
            size.Height = font.Size;
            switch (datum)
            {
                case StringAlignment.Center:
                    point.X -= Convert.ToInt32(Math.Round(size.Width / 2 * 1.1, 0));
                    break;
                case StringAlignment.Far:
                    point.X -= Convert.ToInt32(Math.Round(size.Width * 1.1, 0));
                    break;
                default:
                    break;
            }
            point.Y -= Convert.ToInt32(Math.Round(size.Height / 2 * 1.5));
            g.DrawString(
                str,
                font,
                brush,
                point);
        }


        #endregion
    }
}
