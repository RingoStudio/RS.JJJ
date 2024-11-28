using Newtonsoft.Json.Linq;
using RS.Tools.Common.Enums;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.utils
{
    internal class SnailCDKeyHelper
    {
        private static SnailCDKeyHelper _instance = new SnailCDKeyHelper();
        public const string TAG = "SnailCDKeyHelper";
        public static SnailCDKeyHelper Instance() => _instance;
        private Dictionary<string, CDKey> _keys = new();
        private OSSHelper _oss = new();
        public void GetCDKey(Action<string> callback = null)
        {
            try
            {
                var bytes = _oss.GetData(OSSHelper.PATH_FF);
                if (bytes is null)
                {
                    if (callback is not null) callback("服务器尚未配置任何密令！");
                    return;
                }
                var raw = Encoding.UTF8.GetString(_oss.GetData(OSSHelper.PATH_FF));
                var keys = ConvertCDKeys(raw);
                int newCount = 0;
                int OldCount = 0;
                foreach (var item in keys)
                {
                    if (item.Value.Available)
                    {
                        if (!_keys.ContainsKey(item.Key))
                        {
                            _keys.Add(item.Key, item.Value);
                            newCount++;
                        }
                        else if (!_keys[item.Key].Available)
                        {
                            _keys[item.Key].Available = true;
                            newCount++;
                        }
                    }
                    else
                    {
                        if (_keys.ContainsKey(item.Key))
                        {
                            _keys[item.Key].Available = false;
                            OldCount++;
                        }
                        else
                        {
                            _keys.Add(item.Key, item.Value);
                            OldCount++;
                        }
                    }
                }

                if (callback is not null)
                {
                    var desc = new List<string>
                    {
                        $"新增{newCount}条新密令\n"
                    };
                    if (OldCount > 0) desc.Add($"增加{newCount}条过期密令");
                    callback.Invoke($"本次共{string.Join("，", desc)}。");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteException(ex, TAG);
                if (callback is null) return;
                callback($"下载密令过程中出现错误！详细信息如下\n{ex.Message}");
            }
        }

        public void UploadCDKey(Action<string> callback = null)
        {
            try
            {
                var txt = ConvertCDKeysToString(_keys);
                var ret = _oss.PutData(OSSHelper.PATH_FF, Encoding.UTF8.GetBytes(txt));
                if (callback is not null)
                {
                    if (ret) callback("上传密令成功！");
                    else callback("上传密令失败！");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteException(ex, TAG);
                if (callback is null) return;
                callback($"上传密令过程中出现错误！详细信息如下\n{ex.Message}");
            }
        }


        public int QueryCDKeyPages()
        {
            // 用linq统计_keys中Available为true的数量?

            return _keys.Count(a => a.Value.Available) / 20 + 1;
        }
        public string QueryCDKey(int index = 1)
        {
            var availableCount = _keys.Count(a => a.Value.Available);
            if (availableCount == 0) return "";
            var start = (index - 1) * 20;
            var end = Math.Min(index * 20, availableCount);
            if (end <= start) return "";
            return String.Join("\n", _keys.Values.Where(a => a.Available).Select(a => a.Key).Reverse().ToArray()[start..end]);
        }

        public void load_jo()
        {
            try
            {
                var txt = IOHelper.GetCSV(CSVType.RobotData, robot.include.files.CDKey);
                if (string.IsNullOrEmpty(txt))
                {
                    Console.WriteLine($"没有找到已存储的本地密令。");
                    return;
                };
                _keys = ConvertCDKeys(txt);
                Console.WriteLine($"已加载{_keys.Count}条本地密令。");
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteException(ex, TAG);
            }
        }
        public string save_jo()
        {
            try
            {
                var jo = ConvertCDKeysToString(_keys);
                return IOHelper.SaveCSV(CSVType.RobotData, jo, robot.include.files.CDKey);
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteException(ex, TAG);
                return "";
            }
        }

        private string ConvertCDKeysToString(Dictionary<string, CDKey> cdkeys)
        {
            if (cdkeys is null || cdkeys.Count == 0) return "";
            dynamic jo = new JArray();
            foreach (var item in cdkeys.Values)
            {
                dynamic j = new JObject();
                j.name = item.Key;
                j.available = item.Available;
                j.date = item.Date;
                jo.Add(j);
            }
            return JSONHelper.AnyObject2JString(jo);
        }
        private Dictionary<string, CDKey> ConvertCDKeys(string raw)
        {
            var ret = new Dictionary<string, CDKey>();
            if (string.IsNullOrEmpty(raw)) return ret;
            try
            {
                dynamic jo = JArray.Parse(raw);
                foreach (var item in jo)
                {
                    var Key = JSONHelper.ParseString(item.name);
                    ret.Add(Key, new CDKey()
                    {
                        Key = Key,
                        Available = JSONHelper.ParseBool(item.available),
                        Date = JSONHelper.ParseString(item.date),
                    });
                }

                return ret;
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteException(ex, TAG);
                return new Dictionary<string, CDKey>();
            }
        }

        /// <summary>
        /// 添加密令
        /// </summary>
        /// <param name="cdkeys"></param>
        /// <param name="callback"></param>
        public void AddNewCDKeys(List<string> cdkeys, Action<string> callback)
        {
            if (cdkeys is null || cdkeys.Count == 0) return;
            int newCount = 0;
            int oldCount = 0;
            foreach (var key in cdkeys)
            {
                if (!_keys.ContainsKey(key))
                {
                    _keys.Add(key, new CDKey()
                    {
                        Key = key,
                        Available = true,
                        Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    });
                    newCount++;
                }
                else if (!_keys[key].Available)
                {
                    _keys[key].Available = true;
                    oldCount++;
                }
            }

            if (callback is not null)
            {
                var desc = new List<string>
                {
                    $"新增{newCount}条密令"
                };
                if (oldCount > 0) desc.Add($"恢复{newCount}条密令");
                callback.Invoke($"本次共{string.Join("，", desc)}。");
            }
        }
        /// <summary>
        /// 过期密令
        /// </summary>
        /// <param name="cdkeys"></param>
        public void AddOldCDKeys(List<string> cdkeys, Action<string> callback)
        {
            if (cdkeys is null || cdkeys.Count == 0) return;
            int oldCount = 0;
            foreach (var key in cdkeys)
            {
                if (_keys.ContainsKey(key))
                {
                    _keys[key].Available = false;
                    oldCount++;
                }
                else
                {
                    _keys.Add(key, new CDKey()
                    {
                        Key = key,
                        Available = false,
                        Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    });
                    oldCount++;
                }
            }

            if (callback is not null)
            {
                callback.Invoke($"本次共设置{oldCount}条过期密令。");
            }
        }

    }

    internal class CDKey
    {
        public string Key { get; set; }
        public bool Available { get; set; }
        public string Date { get; set; }
    }
}
