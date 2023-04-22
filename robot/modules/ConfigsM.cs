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
    /// 基础配置模块
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
        /// <summary>
        /// 关闭全局对话相应(除超管)
        /// </summary>
        public bool SwitchCommunicateClose { get; set; }
        /// <summary>
        /// 禁用启动登录
        /// </summary>
        public bool SwitchLoginClose { get; set; }
        /// <summary>
        /// 禁用图鉴
        /// </summary>
        public bool SwitchHandbookClose { get; set; }
        /// <summary>
        /// 禁用密令
        /// </summary>
        public bool SwitchCDKeyClose { get; set; }
        /// <summary>
        /// 禁用抽签
        /// </summary>
        public bool SwitchQianClose { get; set; }
        /// <summary>
        /// 禁用对话
        /// </summary>
        public bool SwitchConversationClose { get; set; }



        /// <summary>
        /// 相同对话间隔
        /// </summary>
        public long CfgSameContentInterval { get; private set; }

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
                    Context.Logger.Write(ex, $"ConfigsM.InitFolders {_folder}");
                }

            }
        }

        private void LoadCSV()
        {
            try
            {
                _commonConfigs = IOHelper.GetJO("BOT\\jjj_cfg.json", false) ?? new JObject();
                CfgSameContentInterval = JSONHelper.ParseLong(_commonConfigs.same_content_cd_second);
                //  _clubConfigs = IOHelper.GetCSV(Tools.Common.Enums.CSVType.RobotData, include.files.Club_Configs) ?? new JObject();
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
