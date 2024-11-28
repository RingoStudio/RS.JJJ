using RS.Snail.JJJ.clone;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.utils
{
    internal class MaydayDrawing
    {
        public static List<string> DrawRecipe(int index, string city, string nick, MaydayTicketToken data)
        {
            try
            {
                var timeCode = TimeHelper.ToTimeStampMills().ToString();
                if (!string.IsNullOrEmpty(nick)) timeCode = $"@{nick}_{timeCode}";
                var imageOutputPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUT", $"处方_{timeCode}.png");

                // 3120*4620
                var imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RES", "mayday", $"recipe_01.png");
                var image = Bitmap.FromFile(imagePath);
                var b = new System.Drawing.Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var g = Graphics.FromImage(b);

                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(Color.Transparent);
                g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), new RectangleF(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
                Point pos = new Point(0, 0);
                var brush = new SolidBrush(System.Drawing.Color.FromArgb(0, 0, 0));
                var alignment = StringAlignment.LEFT;
                var fontStyle = StringFontStyle.Regular;
                double fontSize = 62;

                pos.X = 2140;
                pos.Y = 854;
                var title = $"病歷號碼 : {55550000 + index}";
                DrawStringHorizontal2(g, pos, title, Convert.ToSingle(fontSize), brush, fontStyle);

                var birthday = $"{data.BirthDay[..4]}-{data.BirthDay.Substring(4, 2)}-{data.BirthDay[6..]}";
                var age = DateTime.Now.Year - int.Parse(data.BirthDay[..4]);
                title = $"姓名: {data.AudienceName}  出生日期: {birthday}  年齡: {age}  看诊日期: {DateTime.Now:yyyy-MM-dd}";
                pos.X = 186;
                pos.Y = 1120;
                DrawStringHorizontal2(g, pos, title, Convert.ToSingle(fontSize), brush, fontStyle);

                pos.Y = 1270;
                title = $"診斷: 五月天{city}戒斷綜合征";
                DrawStringHorizontal2(g, pos, title, Convert.ToSingle(fontSize), brush, fontStyle);

                var outputList = new List<string>();
                b.Save(imageOutputPath, System.Drawing.Imaging.ImageFormat.Png);
                outputList.Add(imageOutputPath);
                return outputList;
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteException(ex, "MaydayDrawing.DrawRecipe");
                return null;
            }
        }
        public static List<string> DrawTicket(dynamic format, string nick, MaydayTicketToken data)
        {
            try
            {
                var frontImagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RES", "mayday", $"{JSONHelper.ParseString(format["date"][data.Date])}{(data.IsBleed ? "_bleed" : "")}.png");
                var backImagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RES", "mayday", $"{JSONHelper.ParseString(format["back_image"])}{(data.IsBleed ? "_bleed" : "")}.png");

                var timeCode = TimeHelper.ToTimeStampMills().ToString();
                if (!string.IsNullOrEmpty(nick)) timeCode = $"@{nick}_{timeCode}";
                if (data.IsCompress) System.IO.Directory.CreateDirectory(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUT", $"mayday_{timeCode}"));
                var frontImageOutputPath = data.IsCompress ? System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUT", $"mayday_{timeCode}", $"{JSONHelper.ParseString(format["date"][data.Date])}正面_{timeCode}.png") :
                                                             System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUT", $"{JSONHelper.ParseString(format["date"][data.Date])}正面_{timeCode}.png");
                var backImageOutputPath = data.IsCompress ? System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUT", $"mayday_{timeCode}", $"{JSONHelper.ParseString(format["date"][data.Date])}背面_{timeCode}.png") :
                                                            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUT", $"{JSONHelper.ParseString(format["date"][data.Date])}背面_{timeCode}.png");

                if (!System.IO.File.Exists(frontImagePath)) return null;
                var image = Bitmap.FromFile(frontImagePath);

                var b = new System.Drawing.Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var g = Graphics.FromImage(b);

                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(Color.Transparent);
                g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), new RectangleF(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);

                Point bleedOffset = data.IsBleed ? ParsePosition(format.bleed_offset) : new Point(0, 0);
                foreach (var item in format.format)
                {
                    var name = JSONHelper.ParseString(item.name);
                    Point pos = ParsePosition(item.pos);
                    pos = new Point(pos.X + bleedOffset.X, pos.Y + bleedOffset.Y);
                    var brush = ParseBrush(item.rgb);
                    var alignment = ParseStringAlignment(item.alignment);
                    var fontStyle = ParseFontStyle(item.style);
                    var direction = ParseStringDirenction(item.direction);
                    var fontSize = JSONHelper.ParseInt(item.font_size);
                    var str = "";
                    string time = JSONHelper.ParseString(format.open_time);
                    var dateTime = new DateTime(year: Convert.ToInt32(data.Date.Substring(0, 4)),
                                                month: Convert.ToInt32(data.Date.Substring(4, 2)),
                                                day: Convert.ToInt32(data.Date.Substring(6, 2)),
                                                hour: Convert.ToInt32(time.Split(":").First()),
                                                minute: Convert.ToInt32(time.Split(":").Last()),
                                                second: 0);
                    var fontHeight = JSONHelper.ParseInt(item.font_height);
                    int maxLen = 0;
                    switch (name)
                    {
                        case "position":
                            str = JSONHelper.ParseString(item.position_format);
                            str = str.Replace("{gate}", data.Gate);
                            str = str.Replace("{area}", data.Area);
                            str = str.Replace("{pos}", data.Pos);
                            break;
                        case "date":

                            var dateFormat = JSONHelper.ParseString(item.date_format);
                            str = dateTime.ToString(dateFormat);
                            break;
                        case "price":
                            str = string.Format(JSONHelper.ParseString(item.price_format), data.Price);
                            break;
                        case "no":
                            str = data.No;
                            if (string.IsNullOrEmpty(str)) str = data.Date + (new Random().Next(10000).ToString("0000"));
                            maxLen = JSONHelper.ParseInt(item.max_len);
                            if (maxLen > 0 && str.Length > maxLen) str = str.Substring(0, maxLen);
                            break;
                        case "ticket_no":
                            str = data.TicketNo;
                            if (string.IsNullOrEmpty(str)) str = TimeHelper.ToTimeStamp().ToString();
                            maxLen = JSONHelper.ParseInt(item.max_len);
                            if (maxLen > 0 && str.Length > maxLen) str = str.Substring(0, maxLen);
                            break;
                        case "name":
                            str = data.AudienceName;
                            if (string.IsNullOrEmpty(str)) str = "WMLS";
                            break;
                        case "position_short":
                            var val = Convert.ToInt32(data.Price);
                            if (val <= 0) str = "地球表面";
                            else if (val <= 1100) str = "看台";
                            else str = "内场";
                            break;
                        case "position_pinyin":
                            str = JSONHelper.ParseString(item.position_format);
                            str = str.Replace("{gate}", Pinyin.GetPinyin(data.Gate).ToUpper());
                            str = str.Replace("{area}", Pinyin.GetPinyin(data.Area).ToUpper());
                            break;
                    }

                    if (string.IsNullOrEmpty(str)) return null;
                    if (direction == StringDirection.VERTICAL) DrawStringVerticalIndividual(g, pos, str, fontHeight, Convert.ToSingle(fontSize * 1.5), brush, alignment, fontStyle);
                    else if (direction == StringDirection.VERTICAL_LEFT) DrawStringVerticalLeft(g, b.Width, b.Height, pos, str, Convert.ToSingle(fontSize * 1.5), brush, alignment, fontStyle);
                    else if (direction == StringDirection.VERTICAL_RIGHT) DrawStringVerticalRight(g, b.Width, b.Height, pos, str, Convert.ToSingle(fontSize * 1.5), brush, alignment, fontStyle);
                    else DrawStringHorizontal(g, pos, str, Convert.ToSingle(fontSize * 1.5), brush, alignment, fontStyle);
                }

                var outputList = new List<string>();
                if (!data.IsCompress)
                {
                    if (System.IO.File.Exists(backImagePath))
                    {
                        System.IO.File.Copy(backImagePath, backImageOutputPath, true);
                        outputList.Add(backImageOutputPath);
                    }
                    b.Save(frontImageOutputPath, System.Drawing.Imaging.ImageFormat.Png);
                    outputList.Add(frontImageOutputPath);
                    return outputList;
                }
                else
                {
                    if (System.IO.File.Exists(backImagePath))
                    {
                        System.IO.File.Copy(backImagePath, backImageOutputPath, true);
                    }
                    b.Save(frontImageOutputPath, System.Drawing.Imaging.ImageFormat.Png);
                    var sourcePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUT", $"mayday_{timeCode}");
                    var zipPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OUT", $"五月天门票_{timeCode}.zip");
                    CompressHelper.CompressionFile(sourcePath, zipPath);
                    outputList.Add(zipPath);
                    return outputList;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteException(ex, "MaydayDrawing.DrawTicket");
                return null;
            }
        }

        public enum StringAlignment
        {
            LEFT = 0,
            RIGHT = 2,
            CENTER = 3,
        }

        public enum StringDirection
        {
            HORIZONTAL = 0,
            VERTICAL = 1,
            VERTICAL_LEFT = 2,
            VERTICAL_RIGHT = 3,
        }

        public enum StringFontStyle
        {
            Regular = 0,
            Bold = 1,
            Light = 2,
        }
        /// <summary>
        /// 绘制横向文字
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pos"></param>
        /// <param name="str"></param>
        /// <param name="fontSize"></param>
        /// <param name="brush"></param>
        /// <param name="alignment"></param>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        private static int DrawStringHorizontal(Graphics g, Point pos, string str, float fontSize, Brush brush, StringAlignment alignment, StringFontStyle fontStyle = StringFontStyle.Bold)
        {
            if (string.IsNullOrEmpty(str)) return 0;
            var font = (fontStyle switch
            {
                StringFontStyle.Light => new Font("Microsoft YaHei UI Light", fontSize, FontStyle.Regular, GraphicsUnit.Point),
                StringFontStyle.Regular => new Font("Microsoft YaHei UI", fontSize, FontStyle.Regular, GraphicsUnit.Point),
                _ => new Font("Microsoft YaHei UI", fontSize, FontStyle.Bold, GraphicsUnit.Point)
            });
            var size = g.MeasureString(str, font, new PointF() { X = pos.X, Y = pos.Y }, StringFormat.GenericTypographic);

            size.Height = Convert.ToInt32(fontSize);
            if (alignment == StringAlignment.CENTER) pos.X -= Convert.ToInt32(Math.Round(size.Width / 2d));
            else if (alignment == StringAlignment.RIGHT) pos.X -= Convert.ToInt32(size.Width);

            pos.Y -= Convert.ToInt32(Math.Round(size.Height / 3d));

            g.DrawString(str, font, brush, pos);

            return pos.X + Convert.ToInt32(size.Width);
        }
        private static int DrawStringHorizontal2(Graphics g, Point pos, string str, float fontSize, Brush brush, StringFontStyle fontStyle = StringFontStyle.Bold)
        {
            if (string.IsNullOrEmpty(str)) return 0;
            var nums = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-";
            bool first = true;
            for (int i = 0; i < str.Length; i++)
            {
                var s = str.Substring(i, 1);
                var flag = false;
                if (nums.Contains(s)) flag = true;
                var fontName = flag ? "Times New Roman" : "楷体";
                var font = (fontStyle switch
                {
                    StringFontStyle.Light => new Font(fontName, fontSize, FontStyle.Regular, GraphicsUnit.Point),
                    StringFontStyle.Regular => new Font(fontName, fontSize, FontStyle.Regular, GraphicsUnit.Point),
                    _ => new Font(fontName, fontSize, FontStyle.Bold, GraphicsUnit.Point)
                });
                var size = g.MeasureString(s, font, new PointF() { X = pos.X, Y = pos.Y }, StringFormat.GenericTypographic);
                size.Height = Convert.ToInt32(fontSize);
                if (first) pos.Y -= Convert.ToInt32(Math.Round(size.Height / 3d));
                first = false;
                if (s == " ")
                {
                    pos.X += Convert.ToInt32(fontSize / 2);
                    continue;
                }
                g.DrawString(s, font, brush, new Point(pos.X, pos.Y - (flag ? Convert.ToInt32(Math.Round(size.Height / 10d)) : 0)));
                pos.X += Convert.ToInt32(size.Width);
            }
            return pos.X;
        }
        /// <summary>
        /// 绘制纵向排列文字
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pos"></param>
        /// <param name="str"></param>
        /// <param name="fontHeight"></param>
        /// <param name="fontSize"></param>
        /// <param name="brush"></param>
        /// <param name="alignment"></param>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        private static int DrawStringVerticalIndividual(Graphics g, Point pos, string str, int fontHeight, float fontSize, Brush brush, StringAlignment alignment, StringFontStyle fontStyle = StringFontStyle.Bold)
        {
            if (string.IsNullOrEmpty(str)) return 0;
            var font = (fontStyle switch
            {
                StringFontStyle.Light => new Font("Microsoft YaHei UI Light", fontSize, FontStyle.Regular, GraphicsUnit.Point),
                StringFontStyle.Regular => new Font("Microsoft YaHei UI", fontSize, FontStyle.Regular, GraphicsUnit.Point),
                _ => new Font("Microsoft YaHei UI", fontSize, FontStyle.Bold, GraphicsUnit.Point)
            });
            int offset = 0;
            foreach (var item in str)
            {
                var c = item.ToString();
                var y = pos.Y;
                var x = pos.X;
                var size = g.MeasureString(c, font, new PointF() { X = pos.X, Y = pos.Y }, StringFormat.GenericTypographic);

                size.Height = Convert.ToInt32(fontSize);
                if (alignment == StringAlignment.CENTER) pos.X -= Convert.ToInt32(Math.Round(size.Width / 2d));
                else if (alignment == StringAlignment.RIGHT) pos.X -= Convert.ToInt32(size.Width);

                pos.Y -= Convert.ToInt32(Math.Round(size.Height / 3d));

                g.DrawString(c, font, brush, pos);

                offset = Math.Max(offset, pos.X + Convert.ToInt32(size.Width));
                pos.Y = y + fontHeight;
                pos.X = x;
            }

            return offset;
        }
        private static int DrawStringVerticalLeft(Graphics g, int gWidth, int gHeight, Point pos, string str, float fontSize, Brush brush, StringAlignment alignment, StringFontStyle fontStyle = StringFontStyle.Bold)
        {
            var font = (fontStyle switch
            {
                StringFontStyle.Light => new Font("Microsoft YaHei UI Light", fontSize, FontStyle.Regular, GraphicsUnit.Point),
                StringFontStyle.Regular => new Font("Microsoft YaHei UI", fontSize, FontStyle.Regular, GraphicsUnit.Point),
                _ => new Font("Microsoft YaHei UI", fontSize, FontStyle.Bold, GraphicsUnit.Point)
            });

            var size = g.MeasureString(str, font, new PointF() { X = pos.X, Y = pos.Y }, StringFormat.GenericTypographic);
            size.Height = Convert.ToInt32(fontSize);
            if (alignment == StringAlignment.CENTER) pos.Y -= Convert.ToInt32(Math.Round(size.Width / 2d));
            else if (alignment == StringAlignment.LEFT) pos.Y -= Convert.ToInt32(size.Width);
            // pos.X -= Convert.ToInt32(Math.Round(size.Height / 3d));

            var state = g.Save();
            // g.ResetTransform();
            g.TranslateTransform(gWidth / 2, gHeight / 2);
            g.RotateTransform(-90);

            g.DrawString(str, font, brush, new PointF(gHeight - pos.Y - (size.Width) - gHeight / 2f, pos.X - gWidth / 2f));
            g.Restore(state);
            return pos.X + Convert.ToInt32(size.Height);
        }
        private static int DrawStringVerticalRight(Graphics g, int gWidth, int gHeight, Point pos, string str, float fontSize, Brush brush, StringAlignment alignment, StringFontStyle fontStyle = StringFontStyle.Bold)
        {
            var font = (fontStyle switch
            {
                StringFontStyle.Light => new Font("Microsoft YaHei UI Light", fontSize, FontStyle.Regular, GraphicsUnit.Point),
                StringFontStyle.Regular => new Font("Microsoft YaHei UI", fontSize, FontStyle.Regular, GraphicsUnit.Point),
                _ => new Font("Microsoft YaHei UI", fontSize, FontStyle.Bold, GraphicsUnit.Point)
            });

            var size = g.MeasureString(str, font, new PointF() { X = pos.X, Y = pos.Y }, StringFormat.GenericTypographic);
            size.Height = Convert.ToInt32(fontSize);
            if (alignment == StringAlignment.CENTER) pos.Y -= Convert.ToInt32(Math.Round(size.Width / 2d));
            else if (alignment == StringAlignment.LEFT) pos.Y -= Convert.ToInt32(size.Width);
            // pos.X -= Convert.ToInt32(Math.Round(size.Height / 3d));

            var state = g.Save();
            // g.ResetTransform();
            g.TranslateTransform(gWidth / 2, gHeight / 2);
            g.RotateTransform(90);

            g.DrawString(str, font, brush, new PointF(pos.Y - gHeight / 2f, gWidth - pos.X - size.Height - gWidth / 2f));
            g.Restore(state);
            return pos.X + Convert.ToInt32(size.Height);
        }

        private static Point ParsePosition(dynamic data)
        {
            var arr = JSONHelper.ParseIntList(data);
            if (arr is null || arr.Count != 2) return new Point(0, 0);
            return new Point(arr[0], arr[1]);
        }
        private static SolidBrush ParseBrush(dynamic data)
        {
            var arr = JSONHelper.ParseIntList(data);
            if (arr is null || arr.Count != 3) return new SolidBrush(System.Drawing.Color.FromArgb(0, 0, 0));
            return new SolidBrush(System.Drawing.Color.FromArgb(arr[0], arr[1], arr[2]));
        }

        private static StringAlignment ParseStringAlignment(dynamic data)
        {
            var flag = JSONHelper.ParseString(data);
            return flag switch
            {
                "center" => StringAlignment.CENTER,
                "right" => StringAlignment.RIGHT,
                _ => StringAlignment.LEFT
            };
        }
        private static StringDirection ParseStringDirenction(dynamic data)
        {
            var flag = JSONHelper.ParseString(data);
            return flag switch
            {
                "v1" => StringDirection.VERTICAL,
                "v2" => StringDirection.VERTICAL_LEFT,
                "v3" => StringDirection.VERTICAL_RIGHT,
                _ => StringDirection.HORIZONTAL,
            };
        }
        private static StringFontStyle ParseFontStyle(dynamic data)
        {
            var flag = JSONHelper.ParseString(data);
            return flag switch
            {
                "regular" => StringFontStyle.Regular,
                "light" => StringFontStyle.Light,
                _ => StringFontStyle.Bold,
            };
        }

    }
}
