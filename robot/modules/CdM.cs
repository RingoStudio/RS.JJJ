using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.modules
{
    internal class CdM : IModule
    {
        #region FIELDS
        public string ModuleName { get => "CdM"; }
        public Context _context { get; set; }

        private bool _inited = false;
        public bool Inited { get => _inited; }

        private ConcurrentDictionary<string, long> _cdCache;
        #endregion

        #region INIT
        public CdM(boot.Context context)
        {
            this._context = context;
        }
        public void Init(bool load = false)
        {
            if (_inited) return;
            _inited = true;
            LoadCSV();
            RegistBackup();
        }
        private void RegistBackup()
        {
            _context.BackupM.RegistSaveSessions(ModuleName, SaveCSV);
            _context.BackupM.RemoveBackupSession(Tools.Common.Enums.CSVType.RobotData, include.files.Update_Time);
        }
        private void LoadCSV()
        {
            #region UPDATE TIME
            _cdCache = new();
            dynamic data = IOHelper.GetCSV(Tools.Common.Enums.CSVType.RobotData, include.files.Update_Time);
            foreach (var item in JSONHelper.ParseDicStrLong(data))
            {
                _cdCache[item.Key] = item.Value;
            }
            #endregion
        }

        private void SaveCSV()
        {
            dynamic jo = JObject.FromObject(_cdCache);
            try
            {
                IOHelper.SaveCSV(Tools.Common.Enums.CSVType.RobotData, jo, include.files.Update_Time);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, "CdM.SaveCSV");
            }
        }
        #endregion

        #region PUBLIC METHODS
        public void ClearCache() => _cdCache = new ConcurrentDictionary<string, long>();
        public bool IsInCD(Message msg)
        {
            var key = GetKey(msg);
            var now = TimeHelper.ToTimeStamp();
            var flag = false;
            if (!_cdCache.ContainsKey(key))
            {
                _cdCache.TryAdd(key, now);
            }
            else
            {
                if (_cdCache[key] + _context.ConfigsM.CfgSameContentInterval > now)
                {
                    flag = true;
                }
                else
                {
                    _cdCache[key] = now;
                }

                foreach (var item in _cdCache.Where(a => (a.Value + _context.ConfigsM.CfgSameContentInterval) <= now))
                {
                    if (item.Key.StartsWith("@::_")) continue;
                    _cdCache.TryRemove(item);
                }
            }

            return flag;
        }

        private string GetKey(Message msg)
        {
            return $"{msg.Self}-{msg.Sender}-{msg.Type}-{msg.Content}";
        }
        /// <summary>
        /// 添加自定义key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="time"></param>
        public void SetCache(string key, long time = -1)
        {
            key = $"@::_{key}";
            _cdCache[key] = time <= 0 ? TimeHelper.ToTimeStamp() : time;
        }
        /// <summary>
        /// 获取自定义key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long GetCache(string key)
        {
            key = $"@::_{key}";
            return _cdCache.ContainsKey(key) ? _cdCache[key] : 0;
        }
        #endregion
    }
}
