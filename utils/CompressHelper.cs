using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpCompress.Common;
using SharpCompress.Writers;

namespace RS.Snail.JJJ.utils
{
    internal class CompressHelper
    {
        public static void CompressionFile(string filePath, string zipPath, List<string> filterExtenList = null)
        {
            try
            {
                using (var zip = File.Create(zipPath))
                {
                    var option = new WriterOptions(CompressionType.Deflate)
                    {
                        ArchiveEncoding = new SharpCompress.Common.ArchiveEncoding()
                        {
                            Default = Encoding.UTF8
                        }
                    };
                    using (var zipWriter = WriterFactory.Open(zip, ArchiveType.Zip, option))
                    {
                        if (Directory.Exists(filePath))
                        {
                            //添加文件夹
                            zipWriter.WriteAll(filePath, "*",
                                (path) => filterExtenList == null ? true : !filterExtenList.Any(d => Path.GetExtension(path).Contains(d, StringComparison.OrdinalIgnoreCase)), SearchOption.AllDirectories);
                        }
                        else if (File.Exists(filePath))
                        {
                            zipWriter.Write(Path.GetFileName(filePath), filePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
