using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.utils;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.modules
{
    /// <summary>
    /// 本地对话模块
    /// </summary>
    internal class ConversationM : IModule
    {
        #region FIELDS
        public string ModuleName { get => "ConversationM"; }
        public Context _context { get; set; }

        private bool _inited = false;
        public bool Inited { get => _inited; }

        private ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _conversations;
        private Dictionary<string, string> _fixedConversation = new Dictionary<string, string>()
        {
            { "你是谁", "我是全新升级的唧唧叽2.0" },
            { "你是谁?", "我是全新升级的唧唧叽2.0" },
            { "你是谁？", "我是全新升级的唧唧叽2.0" },
            { "你爸爸是谁", "我的爸爸是机会" },
            { "你爸爸是谁?", "我的爸爸是机会" },
            { "你爸爸是谁？", "我的爸爸是机会" },
        };
        #endregion

        #region INIT
        public ConversationM(boot.Context context)
        {
            this._context = context;
        }
        public void Init(bool load = false)
        {
            if (_inited) return;
            _inited = true;
            LoadCSV();
            RegistBackups();
        }
        /// <summary>
        /// 加载数据
        /// </summary>
        private void LoadCSV()
        {
            _conversations = new();
            try
            {
                var data = IOHelper.GetCSV(Tools.Common.Enums.CSVType.RobotData, include.files.Conversation_CSV) ?? new JObject();
                foreach (var groupItem in data)
                {
                    string groupRID = groupItem.Name;
                    var groupData = groupItem.Value;
                    if (!_conversations.ContainsKey(groupRID)) _conversations.TryAdd(groupRID, new());
                    foreach (var item in groupData)
                    {
                        string key = CryptoHelper.DecryptBase64(item.Name);
                        string value = CryptoHelper.DecryptBase64(JSONHelper.ParseString(item.Value));
                        _conversations[groupRID][key] = value;
                    }
                }
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, $"ConversationM.LoadCSV");
            }
        }

        private void SaveCSV()
        {
            try
            {
                var dic = new Dictionary<string, Dictionary<string, string>>();
                foreach (var groupItem in _conversations)
                {
                    dic.Add(groupItem.Key, new());
                    foreach (var conversation in groupItem.Value)
                    {
                        dic[groupItem.Key][CryptoHelper.EncryptBase64(conversation.Key)] = CryptoHelper.EncryptBase64(conversation.Value);
                    }
                }
                dynamic jo = JObject.FromObject(dic);
                IOHelper.SaveCSV(Tools.Common.Enums.CSVType.RobotData, jo, include.files.Conversation_CSV);
            }
            catch (Exception ex)
            {
                Context.Logger.Write(ex, $"ConversationM.SaveCSV");
            }

        }

        private void RegistBackups()
        {
            _context.BackupM.RegistSaveSessions(ModuleName, SaveCSV);
            _context.BackupM.RegistBackupSession(Tools.Common.Enums.CSVType.RobotData, include.files.Conversation_CSV);
        }
        #endregion

        #region METHODS
        /// <summary>
        /// 更新全局对话
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool UpdateGlobalConversation(string key, string value)
        {
            if (!_conversations.ContainsKey("")) _conversations.TryAdd("", new());
            if (string.IsNullOrEmpty(value))
            {
                if (_conversations[""].ContainsKey(key)) _conversations[""].Remove(key, out _);
                else return false;
            }
            else _conversations[""][key] = value;
            return true;
        }
        /// <summary>
        /// 更新对话
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="rid"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool UpdateGroupConversation(string rid, string key, string value)
        {
            if (!_conversations.ContainsKey(rid)) _conversations.TryAdd(rid, new());
            if (string.IsNullOrEmpty(value))
            {
                if (_conversations[rid].ContainsKey(key)) _conversations[rid].Remove(key, out _);
                else return false;
            }
            else _conversations[rid][key] = value;
            return true;
        }
        /// <summary>
        /// 查询对话
        /// </summary>
        /// <param name="robotWxid"></param>
        /// <param name="rid"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public string? QueryConversation(string rid, string wxid, string key, bool isForce = false)
        {
            key = RemoveJJJ(key);
            if (string.IsNullOrEmpty(key)) return null;

            if (_fixedConversation.ContainsKey(key)) return _fixedConversation[key];

            if (_conversations.ContainsKey(rid))
            {
                if (_conversations[rid].ContainsKey(key)) return _conversations[rid][key];
            }
            if (!string.IsNullOrEmpty(rid))
            {
                if (_conversations.ContainsKey("") && _conversations[""].ContainsKey(key))
                {
                    return _conversations[""][key];
                }
            }
            // TODO: 青云客 API
            return QingyunkeHelper.GetResponse(_context, key, wxid, isForce);
        }

        private static string RemoveJJJ(string raw)
        {
            raw = raw.Replace("@", "").Replace($"{(char)0x85}", "").Trim();
            if (raw.Length < 3) return raw;
            var pinyin = Pinyin.GetPinyin(raw).ToLower();
            if (pinyin == "jijiji") return "";
            var ret = new List<char>();
            var words = raw.ToList().Select(a => Pinyin.GetPinyin(a).ToLower()).ToList();
            for (int i = 0; i < words.Count; i++)
            {
                if (i < words.Count - 2 && words[i] == "ji" && words[i + 1] == "ji" && words[i + 2] == "ji") i += 2;
                else ret.Add(raw[i]);
            }
            return string.Join("", ret);
        }
        #endregion
    }
}
