using Aliyun.OSS.Util;
using Org.BouncyCastle.Utilities;
using RS.Snail.JJJ.boot;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace RS.Snail.JJJ.robot.modules
{
    internal class ScheduleM : IModule
    {
        #region FIELDS
        public string ModuleName { get => "ScheduleM"; }
        public Context _context { get; set; }

        private bool _inited = false;
        public bool Inited { get => _inited; }

        //private long _gameStartTime = RS.Snail.JJJ.Client.core.game.include.config.GAME_START_TIME;

        private List<ScheduleSession> _sessions = new();
        private object _lock = new object();
        private System.Timers.Timer _timer;

        #endregion

        #region INIT
        public ScheduleM(boot.Context context)
        {
            this._context = context;
        }
        public void Init(bool load = false)
        {
            if (_inited) return;
            _inited = true;

        }
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// 注册自动任务
        /// </summary>
        /// <param name="tag">任务标签</param>
        /// <param name="times">时间点列表，格式“周几-小时-分钟-秒”</param>
        /// <param name="action">任务方法</param>
        /// <param name="isAutoLogin">是否是自动登录</param>
        public void RegistSession(string tag, List<string> times, Action action, bool isAutoLogin)
        {
            _sessions.Add(new ScheduleSession(tag, times, 0, action, isAutoLogin, include.ScheduleType.WEEK));
        }

        public void RegistSession(string tag, long intreval, Action action, bool isAutoLogin)
        {
            lock (_lock)
            {
                _sessions.Add(new ScheduleSession(tag, null, intreval, action, isAutoLogin, include.ScheduleType.INTERVAL));
            }
        }

        public void UnregistSession(string tag)
        {
            lock (_lock)
            {
                _sessions = _sessions.Where((a) => a.Tag != tag).ToList();
            }
        }

        public bool CheckTimeClose(string tag = "", bool justLogin = false, long interval = 60)
        {
            var now = TimeHelper.ToTimeStamp();
            foreach (var session in _sessions.ToList())
            {
                if (!string.IsNullOrEmpty(tag) && session.Tag == tag && session.CheckTimeClose(justLogin, now, interval)) return true;
                else if (session.CheckTimeClose(justLogin, now, interval)) return true;
            }
            return false;
        }
        #endregion

        #region Options
        public void StartExecute()
        {
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += Execute;
            _timer.Start();
        }
        public void StopExecute()
        {
            if (_timer is null) return;
            _timer.Elapsed -= Execute;
            _timer.Stop();
        }
        private void Execute(object? ob, ElapsedEventArgs e)
        {
            lock (_lock)
            {
                if (_sessions.Count == 0) return;

                var now = TimeHelper.ToTimeStamp();
                foreach (var session in _sessions.ToList())
                {
                    if (session.CanExecute(now)) session.Execute();
                }
            }
        }
        #endregion

        internal class ScheduleSession
        {
            public string Tag { get; set; }
            /// <summary>
            /// 时间节点（周期为一周）
            /// </summary>
            public List<(int weekday, int hour, int min, int sec)> TimeStones { get; set; }
            /// <summary>
            /// 上一次执行时间
            /// </summary>
            public float LastTime { get; set; }
            /// <summary>
            /// 要执行的任务
            /// </summary>
            public Action Session { get; set; }
            /// <summary>
            /// 是否是登录任务
            /// </summary>
            public bool isAutoLogin { get; set; }
            public long Interval { get; set; }

            public include.ScheduleType ScheduleType { get; set; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="time"></param>
            /// <param name="action"></param>
            /// <param name="isAutoLogin"></param>
            public ScheduleSession(string tag, List<string> time, long interval, Action action, bool isAutoLogin, include.ScheduleType scheduleType)
            {
                Tag = tag;
                TimeStones = time is null ? new List<(int weekday, int hour, int min, int sec)>() : time.Select((a) => ConvertTimeStr(a)).ToList();
                this.Session = action;
                this.LastTime = TimeHelper.ToTimeStamp();
                this.isAutoLogin = isAutoLogin;
                ScheduleType = scheduleType;
                Interval = interval;
            }
            //public void Update(float offset = 0.1f) => time -= offset;
            /// <summary>
            /// 开辟新进程并执行任务
            /// </summary>
            public void Execute()
            {
                LastTime = TimeHelper.ToTimeStamp();
                Task.Run(Session);
            }

            /// <summary>
            /// 是否可以执行该任务
            /// </summary>
            public bool CanExecute(long now = -1)
            {
                now = now <= 0 ? TimeHelper.ToTimeStamp() : now;

                if (ScheduleType == include.ScheduleType.WEEK)
                {
                    // lastTime 超过现在的时间或之前30秒，不可执行
                    if (LastTime >= now - 30) return false;

                    foreach (var time in TimeStones.Select((a) => ConvertTimeStamp(a)))
                    {
                        if (Math.Abs(time - now) <= 10) return true;
                    }
                }
                else if (ScheduleType == include.ScheduleType.INTERVAL)
                {
                    if (LastTime >= now - Interval) return false;
                    return true;
                }

                return false;
            }

            public bool CheckTimeClose(bool justCheckLogin = false, long now = -1, long interval = 60)
            {
                if (justCheckLogin && !isAutoLogin) return false;
                if (ScheduleType == include.ScheduleType.INTERVAL) return false;

                now = now <= 0 ? TimeHelper.ToTimeStamp() : now;
                foreach (var time in TimeStones.Select((a) => ConvertTimeStamp(a)))
                {
                    if (Math.Abs(time - now) <= interval) return true;
                }
                return false;
            }
            private (int weekday, int hour, int min, int sec) ConvertTimeStr(string str)
            {
                int weekday = 0, hour = 0, min = 0, sec = 0;
                str = str.Replace(":", "-");
                var arr = str.Split("-");
                for (int i = 0; i < arr.Length; i++)
                {
                    if (i == 0) weekday = Convert.ToInt32(arr[i]);
                    else if (i == 1) hour = Convert.ToInt32(arr[i]);
                    else if (i == 2) min = Convert.ToInt32(arr[i]);
                    else if (i == 3) sec = Convert.ToInt32(arr[i]);
                }
                return (weekday, hour, min, sec);
            }

            private long ConvertTimeStamp((int weekday, int hour, int min, int sec) time, long now = -1)
            {
                if (now <= 0) now = TimeHelper.ToTimeStamp();
                var start = RS.Snail.JJJ.Client.core.game.include.config.GAME_START_TIME - 3600 * 5;
                var perWeek = RS.Snail.JJJ.Client.core.game.include.config.SECONDS_PER_WEEK;
                now = ((now - start) / perWeek) * perWeek + start;
                now += time.weekday switch
                {
                    6 => 1,
                    7 => 2,
                    1 => 3,
                    2 => 4,
                    3 => 5,
                    4 => 6,
                    _ => 0
                } * RS.Snail.JJJ.Client.core.game.include.config.SECONDS_PER_DAY;
                now += time.hour * 3600 + time.min * 60 + time.sec;

                return now;
            }
        }
    }
}
