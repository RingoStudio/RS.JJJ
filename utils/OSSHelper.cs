using Aliyun.OSS.Common;
using Aliyun.OSS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.utils
{
    internal class OSSHelper
    {
        private static string _accessKeyId = "LTAI5tGttNY2QqcbTGwRKFVC";
        private static string _accessKeySecret = "Yx3sSf3blAMj16CRm7HwidPwbNYmy0";

        private static string _endpoint = "oss-cn-shanghai.aliyuncs.com";
        private static string _bucketName = "ringostudio";
        private static string _root = "jjj/";

        public static string PATH_FF = @"cdkey/cdkey.txt";


        private OssClient _client;
        private ClientConfiguration _config;
        private object _syncLock = new object();

        private static string HmacSha1Sign(string strOrgData)
        {
            var hmacsha1 = new HMACSHA1(Encoding.UTF8.GetBytes(_accessKeySecret));
            var dataBuffer = Encoding.UTF8.GetBytes(strOrgData);
            var hashBytes = hmacsha1.ComputeHash(dataBuffer);
            return Convert.ToBase64String(hashBytes);
        }
        private static string GetAuthorization(string method, string contentType, string path, string time)
        {
            var str = $"{method}\n\n{contentType}\n{time}\n/{_bucketName}/{path}";
            str = HmacSha1Sign(str);
            return $"OSS {_accessKeyId}:{str}";
        }

        public static string ToGMTString()
        {
            return DateTime.Now.ToUniversalTime().ToString("r");
        }
        public static OSSHeader OSSGetHeader(string path)
        {
            var time = ToGMTString();
            var method = "GET";
            var contentType = "application/json";
            return new OSSHeader()
            {
                Method = method,
                Date = time,
                ContentType = contentType,
                Authorization = GetAuthorization(method,
                                                 contentType,
                                                 path,
                                                 time),
                Url = $"https://{_bucketName}.{_endpoint}/{path}",
            };
        }

        public OSSHelper()
        {
            _client = new OssClient(_endpoint, _accessKeyId, _accessKeySecret);
        }

        public byte[] GetData(string fileName)
        {
            lock (_syncLock)
            {
                if (string.IsNullOrEmpty(fileName)) return null;
                fileName = _root + fileName;
                fileName = fileName.Replace("\\", "/");
                try
                {
                    var obj = _client.GetObject(_bucketName, fileName);
                    var data = new List<byte>();
                    using (var requestStream = obj.Content)
                    {
                        int idx;
                        do
                        {
                            idx = requestStream.ReadByte();
                            if (idx == -1) break;
                            data.Add((byte)idx);
                        } while (true);
                    }
                    return data.ToArray();
                }
                catch (Exception ex)
                {
                    if (ex.Message == "The specified key does not exist") return null;
                    return null;
                }
            }
        }

        public bool PutData(string fileName, byte[] data)
        {
            lock (_syncLock)
            {
                if (string.IsNullOrEmpty(fileName)) return false;
                fileName = _root + fileName;
                fileName = fileName.Replace("\\", "/");
                try
                {
                    _client.PutObject(_bucketName, fileName, new System.IO.MemoryStream(data));
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
        }
    }
    public class OSSHeader
    {
        public string Url { get; set; }
        public string Method { get; set; } = "GET";
        public int Timeout { get; set; } = 15000;
        public string ContentType { get; set; } = "application/json";
        public string Date { get; set; }
        public string Authorization { get; set; }
    }
}
