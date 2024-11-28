using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.X509;
using RS.Snail.JJJ.boot;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.modules
{
    /// <summary>
    /// 基础配置模组
    /// </summary>
    internal class ConfigsM : IModule
    {
        #region FIELDS
        public string ModuleName { get => "ConfigsM"; }
        public Context _context { get; set; }

        private bool _inited = false;
        public bool Inited { get => _inited; }

        private dynamic _commonConfigs;
        private dynamic _clubConfigs;

        private static List<string> _folders = new List<string>
        {
            @"LOG",
            @"BOT",
            @"OUT",
            @"RES",
            @"WECHAT",
            @"RES\FORMAL",
            @"RES\GUANGZI",
            @"RES\TAIWAN",
            @"RES\LANGUAGE",
            @"RES\IMG",
            @"RES\FONTS",
            @"USER",
            @"USER\CLUB",
            @"USER\PKGS",
            @"USER\SETTING",
            @"USER\CACHE",
            @"USER\DATA",
        };
        #endregion

        #region GLOBAL SETTINGS

        private bool _switchCommunicateClose = false;
        /// <summary>
        /// 关闭全局对话相应(除超管)
        /// </summary>
        public bool SwitchCommunicateClose
        {
            get => _switchCommunicateClose;
            set
            {
                _switchCommunicateClose = value;
                _commonConfigs.switch_communicate_close = _switchCommunicateClose;
                SaveCSV();
            }
        }

        private bool _switchLoginClose = false;
        /// <summary>
        /// 禁用启动登录
        /// </summary>
        public bool SwitchLoginClose
        {
            get => _switchLoginClose;
            set
            {
                _switchLoginClose = value;
                _commonConfigs.switch_login_close = _switchLoginClose;
                SaveCSV();
            }
        }

        private bool _switchHandbookClose = false;
        /// <summary>
        /// 禁用图鉴
        /// </summary>
        public bool SwitchHandbookClose
        {
            get => _switchHandbookClose;
            set
            {
                _switchHandbookClose = value;
                _commonConfigs.switch_handbook_close = _switchHandbookClose;
                SaveCSV();
            }
        }

        private bool _switchCDKeyClose = false;
        /// <summary>
        /// 禁用密令
        /// </summary>
        public bool SwitchCDKeyClose
        {
            get => _switchCDKeyClose;
            set
            {
                _switchCDKeyClose = value;
                _commonConfigs.switch_cdkey_close = _switchCDKeyClose;
                SaveCSV();
            }
        }

        private bool _switchQianClose = false;
        /// <summary>
        /// 禁用抽签
        /// </summary>
        public bool SwitchQianClose
        {
            get => _switchQianClose; set
            {
                _switchQianClose = value;
                _commonConfigs.switch_qian_close = _switchQianClose;
                SaveCSV();
            }
        }

        private bool _switchConversationClose = false;
        /// <summary>
        /// 禁用对话
        /// </summary>
        public bool SwitchConversationClose
        {
            get => _switchConversationClose; set
            {
                _switchConversationClose = value;
                _commonConfigs.switch_conversation_close = _switchConversationClose;
                SaveCSV();
            }
        }

        public int MaydayTicketCountPerday { get; private set; }

        /// <summary>
        /// 相同对话间隔
        /// </summary>
        public long CfgSameContentInterval { get; private set; }
        /// <summary>
        /// 俱乐部登录超时时间
        /// </summary>
        public long ClubLoginTimeoutInterval { get; private set; }
        /// <summary>
        /// 图鉴CD
        /// </summary>
        public long HandbookInterval { get; private set; }

        /// <summary>
        /// OUT文件夹文件过期时间
        /// </summary>
        public long OUTFilesTimeout { get; private set; }
        #endregion

        #region INIT
        public ConfigsM(boot.Context context)
        {
            this._context = context;
        }
        public void Init(bool load = false)
        {
            if (_inited) return;
            _inited = true;
            InitFolders();
            LoadCSV();
        }

        /// <summary>
        /// 补全必要目录
        /// </summary>
        private void InitFolders()
        {
            foreach (var _folder in _folders)
            {
                try
                {
                    if (!System.IO.Directory.Exists(_folder))
                    {
                        System.IO.Directory.CreateDirectory(_folder);
                    }
                }
                catch (Exception ex)
                {
                    Context.Logger.WriteException(ex, $"ConfigsM.InitFolders {_folder}");
                }

            }
        }

        private void LoadCSV()
        {
            try
            {
                _commonConfigs = IOHelper.GetJO("BOT\\jjj_cfg.json", false) ?? new JObject();
                CfgSameContentInterval = JSONHelper.ParseLong(_commonConfigs.same_content_cd_second);
                MaydayTicketCountPerday = JSONHelper.ParseInt(_commonConfigs.mayday_ticket_count_perday);
                _switchCommunicateClose = JSONHelper.ParseBool(_commonConfigs.switch_communicate_close);
                _switchLoginClose = JSONHelper.ParseBool(_commonConfigs.switch_login_close);
                _switchHandbookClose = JSONHelper.ParseBool(_commonConfigs.switch_handbook_close);
                _switchCDKeyClose = JSONHelper.ParseBool(_commonConfigs.switch_cdkey_close);
                _switchQianClose = JSONHelper.ParseBool(_commonConfigs.switch_qian_close);
                _switchConversationClose = JSONHelper.ParseBool(_commonConfigs.switch_conversation_close);
                ClubLoginTimeoutInterval = JSONHelper.ParseLong(_commonConfigs.login_timeout_interval);
                if (ClubLoginTimeoutInterval <= 0) ClubLoginTimeoutInterval = 300;
                HandbookInterval = JSONHelper.ParseLong(_commonConfigs.handbook_interval);
                if (HandbookInterval <= 0) HandbookInterval = 30;
                OUTFilesTimeout = JSONHelper.ParseLong(_commonConfigs.out_files_timeout);
                if (OUTFilesTimeout <= 0) OUTFilesTimeout = 3 * 86400;

                //  _clubConfigs = IOHelper.GetCSV(Tools.Common.Enums.CSVType.RobotData, include.files.Club_Configs) ?? new JObject();
            }
            catch (Exception ex)
            {

            }
        }

        private void SaveCSV()
        {
            try
            {
                IOHelper.SaveJO(_commonConfigs, "BOT\\jjj_cfg.json", false);
            }
            catch (Exception ex)
            {

            }
        }
        #endregion

        #region METHODS
        public dynamic QueryCommon(string path, string path2 = "")
        {
            return JSONHelper.QueryCommon(_commonConfigs, path, path2);
        }


        #endregion

    }
}
