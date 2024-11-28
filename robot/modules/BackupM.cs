using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.game.include;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.modules
{
    /// <summary>
    /// 备份模组
    /// </summary>
    internal class BackupM : IModule
    {
        #region FIELDS
        public string ModuleName { get => "BackupM"; }
        public Context _context { get; set; }

        private bool _inited = false;
        public bool Inited { get => _inited; }

        private List<string> _backupSessions;
        private Dictionary<string, Action> _saveSessions;
        #endregion

        #region INIT
        public BackupM(boot.Context context)
        {
            this._context = context;
        }
        public void Init(bool load = false)
        {
            if (_inited) return;
            _inited = true;

            ClearOUTFiles();

            _backupSessions = new();
            _saveSessions = new();
            RegistSchedule();
        }

        private void RegistSchedule()
        {
            long interval = JSONHelper.ParseLong(_context.ConfigsM.QueryCommon("backup_data_interval_second"));
            _context.ScheduleM.RegistSession(ModuleName, interval, ExecuteBackup, false);
            interval = JSONHelper.ParseLong(_context.ConfigsM.QueryCommon("save_data_interval_second"));
            _context.ScheduleM.RegistSession(ModuleName, interval, ExecuteSave, false);

        }

        public void SaveNow()
        {
            ExecuteSave();
            ExecuteBackup();
        }

        #endregion

        #region METHODS
        public void RegistBackupSession(RS.Tools.Common.Enums.CSVType csvType, string fileName)
        {
            var path = IOHelper.GetCSVPathByType(csvType, fileName).path;
            if (!_backupSessions.Contains(path)) _backupSessions.Add(path);
        }
        public void RemoveBackupSession(RS.Tools.Common.Enums.CSVType csvType, string fileName)
        {
            var path = IOHelper.GetCSVPathByType(csvType, fileName).path;
            if (_backupSessions.Contains(path)) _backupSessions.Remove(path);
        }

        public void RegistSaveSessions(string tag, Action session)
        {
            if (_saveSessions.ContainsKey(tag)) _saveSessions[tag] = session;
            else _saveSessions.Add(tag, session);
        }
        public void UnregistSaveSessions(string tag)
        {
            if (_saveSessions.ContainsKey(tag)) _saveSessions.Remove(tag);
        }
        private void ExecuteBackup()
        {
            foreach (var path in _backupSessions)
            {
                if (!System.IO.File.Exists(path)) continue;
                try
                {
                    var folder = System.IO.Path.Combine(
                          IOHelper.GetFileRoot(path),
                          IOHelper.GetFileNameWithoutExtension(path)
                          );
                    if (!System.IO.Directory.Exists(folder))
                        System.IO.Directory.CreateDirectory(folder);

                    var newPath = System.IO.Path.Combine(folder, $"{IOHelper.GetFileNameWithoutExtension(path)}_{TimeHelper.ToTimeStamp()}.bak");
                    System.IO.File.Copy(path, newPath);
                }
                catch (Exception ex)
                {
                    // Context.Logger.Write(ex, $"BotCore.ExecuteBackup {path}");
                }
            }
        }

        private void ExecuteSave()
        {
            foreach (var item in _saveSessions)
            {
                try
                {
                    item.Value.Invoke();
                }
                catch (Exception ex)
                {
                    Context.Logger.WriteException(ex, $"BotCore.ExecuteSave {item.Key}");

                }
            }
        }
        #endregion

        #region OUT FILES

        /// <summary>
        /// 自动清理过期文件
        /// </summary>
        public string ClearOUTFiles()
        {
            try
            {
                var root = "OUT";
                if (!System.IO.Directory.Exists(root)) return "OUT目录不存在";

                var dir = new System.IO.DirectoryInfo(root);
                int succCnt = 0, failCnt = 0;
                long now = TimeHelper.ToTimeStamp();
                var list = new List<string>();
                foreach (var fileInfo in dir.GetFiles())
                {
                    var time = TimeHelper.ToTimeStamp(fileInfo.LastWriteTime);
                    if (time + _context.ConfigsM.OUTFilesTimeout < now)
                    {
                        list.Add(fileInfo.FullName);
                    }
                }

                if (list.Count == 0) return "没有需要删除的过期文件。";

                foreach (var path in list)
                {
                    try
                    {
                        System.IO.File.Delete(path);
                        succCnt++;
                    }
                    catch (Exception ex1)
                    {
                        Context.Logger.WriteException(ex1, $"BotCore.ClearOUTFiles.Path:{path}");
                        failCnt++;
                    }
                }
                return $"清理过期文件 {succCnt} 个" + (failCnt > 0 ? $"，失败 {failCnt} 个。" : "。");
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, $"BotCore.ClearOUTFiles");
                return "清理过期文件失败，详情请见日志。";
            }
        }


        #endregion
    }
}
