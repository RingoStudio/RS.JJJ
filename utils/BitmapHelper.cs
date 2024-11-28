//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace RS.Snail.JJJ.utils
//{
//    internal class BitmapHelper
//    {  /// <summary>
//       /// 从资源加载图片
//       /// </summary>
//       /// <param name="name"></param>
//       /// <returns></returns>
//        public static BitmapSource LoadResource(string name)
//        {
//            string path = $@"pack://application:,,,/RS.Snail.SSE.WPF;component/ui/res/img/{name}.png";
//            try
//            {
//                return new BitmapImage(new Uri(path, UriKind.Absolute));
//            }
//            catch (Exception ex)
//            {
//                configs.Logger.Instence().WriteException(ex, "LoadResource");
//                return null;
//            }
//        }
//        public static List<BitmapSource> LoadBitmapSources(string name, int count)
//        {
//            var ret = new List<BitmapSource>();
//            for (int i = 1; i <= count; i++)
//            {
//                string path = $@"pack://application:,,,/RS.Snail.SSE.WPF;component/ui/res/img/{name}/{i}.png";
//                ret.Add(new BitmapImage(new Uri(path, UriKind.Absolute)));
//            }
//            return ret;
//        }
//        public static List<BitmapSource> CutBitmapFromResource(string name, int perWidth, int perHeight, int count)
//        {
//            string path = $@"pack://application:,,,/RS.Snail.SSE.WPF;component/ui/res/img/{name}.png";
//            BitmapSource bitSource = new BitmapImage(new Uri(path, UriKind.Absolute));
//            Bitmap bitmap = BitmapSourceToBitmap(bitSource);
//            bitSource = BitmapToBitmapSource(bitmap);

//            var ret = new List<BitmapSource>();

//            for (int i = 0; i < count; i++)
//            {
//                try
//                {
//                    //定义切割矩形
//                    var cut = new Int32Rect(i * perWidth, 0, perWidth, perHeight);
//                    ret.Add(CutImage(bitSource, cut));
//                }
//                catch (Exception)
//                {
//                    continue;
//                }
//            }
//            return ret;
//        }

//        public static BitmapSource CutImage(BitmapSource bitmapSource, Int32Rect cut)
//        {
//            //计算Stride
//            var stride = bitmapSource.Format.BitsPerPixel * cut.Width / 8;
//            //声明字节数组
//            byte[] data = new byte[cut.Height * stride];
//            //调用CopyPixels
//            bitmapSource.CopyPixels(cut, data, stride, 0);

//            return BitmapSource.Create(cut.Width, cut.Height, 0, 0, PixelFormats.Bgr32, null, data, stride);
//        }

//        // ImageSource --> Bitmap
//        public static System.Drawing.Bitmap BitmapSourceToBitmap(ImageSource imageSource)
//        {
//            BitmapSource m = (BitmapSource)imageSource;

//            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(m.PixelWidth,
//                m.PixelHeight,
//                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

//            System.Drawing.Imaging.BitmapData data = bmp.LockBits(
//            new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp.Size),
//            System.Drawing.Imaging.ImageLockMode.WriteOnly,
//            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

//            m.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride); bmp.UnlockBits(data);

//            return bmp;
//        }

//        // Bitmap --> BitmapImage
//        public static BitmapSource BitmapToBitmapSource(Bitmap bitmap)
//        {
//            if (bitmap is null) return null;
//            System.IntPtr hBitmap = bitmap.GetHbitmap();
//            try
//            {
//                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, System.IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
//            }
//            finally
//            {
//                DeleteObject(hBitmap);
//            }

//        }

//        #region IMAGE MANAGE
//        public static BitmapSource ByteToBitmapSource(byte[] data)
//        {
//            if (data is null || data.Length == 0) return null;
//            BitmapImage img = null;
//            try
//            {
//                img = new BitmapImage();
//                img.BeginInit();
//                img.StreamSource = new MemoryStream(data);
//                img.EndInit();
//            }
//            catch (Exception ex)
//            {
//                configs.Logger.Instence().WriteException(ex, "IMG MGR");
//                return null;
//            }
//            return img;
//        }


//        /// <summary>
//        /// 从文件加载图片
//        /// </summary>
//        /// <param name="fileName"></param>
//        /// <returns></returns>
//        public static BitmapSource LoadFile(string fileName, bool packed = false, bool isFullPath = false)
//        {
//            if (!isFullPath && !fileName.StartsWith(@$"RES\IMG\")) fileName = @$"RES\IMG\{fileName}";
//            if (!isFullPath && !fileName.EndsWith(".png")) fileName += ".png";
//            if (packed) return BitmapToBitmapSource(DrawingHelper.LoadFilePacked(fileName));
//            else return ByteToBitmapSource(CryptoHelper.PNGLoad(fileName));
//        }

//        public static void SetGrayscale(System.Windows.Controls.Image img)
//        {
//            // Set image grayscale 
//            img.IsEnabled = false;

//            FormatConvertedBitmap bitmap = new FormatConvertedBitmap();
//            bitmap.BeginInit();
//            bitmap.Source = (BitmapSource)img.Source;
//            bitmap.DestinationFormat = PixelFormats.Gray32Float;
//            bitmap.EndInit();

//            img.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { img.Source = bitmap; }));
//        }

//        #endregion

//        #region WINDOWS DRAWING
//        [System.Runtime.InteropServices.DllImport("Gdi32.dll")]
//        private static extern bool DeleteObject(System.IntPtr hObject);
//        #endregion
//    }
//}
