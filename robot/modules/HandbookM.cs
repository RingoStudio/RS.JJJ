using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client;
using RS.Snail.JJJ.clone;
using RS.Snail.JJJ.robot.include;
using RS.Snail.JJJ.utils;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.modules
{
    internal class HandbookM : IModule
    {
        #region FIELDS
        public string ModuleName { get => "HandbookM"; }
        public Context _context { get; set; }

        private bool _inited = false;
        public bool Inited { get => _inited; }
        private Dictionary<string, CDKey> _cdKeys;
        private Dictionary<int, HandBookVisitors> _visitors;
        private OSSHelper _oss;
        private object cdKeyLock = new object();
        #endregion

        #region INIT
        public HandbookM(Context context)
        {
            this._context = context;
        }
        public void Init(bool load = false)
        {
            if (_inited) return;
            _inited = true;
            InitOSS();
            LoadCSV();
            RegistBackups();
        }
        private void InitOSS()
        {
            var iniPath = "BOT\\oss.ini";
            var accessKeyID = RS.Tools.Common.Utils.IniHelper.Read(iniPath, "oss", "access_key_id", "");
            var accessKeySecret = RS.Tools.Common.Utils.IniHelper.Read(iniPath, "oss", "access_key_secret", "");
            var endPoint = RS.Tools.Common.Utils.IniHelper.Read(iniPath, "oss", "endpoint", "");
            var bucketName = RS.Tools.Common.Utils.IniHelper.Read(iniPath, "oss", "bucket_name", "");
            _oss = new(accessKeyID, accessKeySecret, endPoint, bucketName);
        }
        private void LoadCSV()
        {

            LoadCDKey();
            LoadVisitors();
        }

        public void SaveCSV()
        {
            SaveCDKey();
        }
        private void RegistBackups()
        {
            _context.BackupM.RegistSaveSessions(ModuleName, SaveCSV);
        }
        private void RegistSchedules()
        {
            if ((_context.WechatM.WechatCFG()?.OurName ?? "") == "冰法集团")
            {
                _context.ScheduleM.RegistSession(ModuleName,
                                            times: new List<string>
                                            {
                                                "6-7-0-0",
                                            },
                                            SyncCDKeys,
                                            isAutoLogin: false);
            }
        }
        #endregion

        #region CDKEY
        private void LoadCDKey()
        {
            dynamic data;
            #region cdkey
            try
            {
                data = IOHelper.GetCSV(Tools.Common.Enums.CSVType.UserData, include.files.CDKey) ?? new JArray();
                _cdKeys = new();
                foreach (var item in data)
                {
                    string key = JSONHelper.ParseString(item.name);
                    _cdKeys[key] = new()
                    {
                        Key = key,
                        Available = JSONHelper.ParseBool(item.available),
                        Date = JSONHelper.ParseString(item.date),
                    };
                }
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, $"HandbookM.LoadCSV.CDKey");
            }
            #endregion
        }
        private void SaveCDKey()
        {
            dynamic data;
            try
            {
                data = new JArray();
                foreach (var item in _cdKeys.Values)
                {
                    dynamic jo = new JObject();
                    jo.name = item.Key;
                    jo.available = item.Available;
                    jo.date = item.Date;
                    data.Add(jo);
                }
                IOHelper.SaveCSV(Tools.Common.Enums.CSVType.UserData, data, include.files.CDKey);
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, $"HandbookM.SaveCSV.CDKey");

            }
        }
        public (bool result, string? desc) AddNewCDKeys(List<string> cdkeys)
        {
            if (cdkeys is null || cdkeys.Count == 0) return (false, null);
            int newCount = 0;
            int oldCount = 0;
            foreach (var key in cdkeys)
            {
                if (!_cdKeys.ContainsKey(key))
                {
                    _cdKeys.Add(key, new CDKey()
                    {
                        Key = key,
                        Available = true,
                        Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    });
                    newCount++;
                }
                else if (!_cdKeys[key].Available)
                {
                    _cdKeys[key].Available = true;
                    oldCount++;
                }
            }
            var desc = new List<string> { $"新增{newCount}条密令" };
            if (oldCount > 0) desc.Add($"恢复{oldCount}条密令");
            return (true, $"本次共{string.Join("，", desc)}。");
        }/// <summary>
         /// 过期密令
         /// </summary>
         /// <param name="cdkeys"></param>
        public (bool result, string? desc) AddOldCDKeys(List<string> cdkeys)
        {
            if (cdkeys is null || cdkeys.Count == 0) return (false, null);
            int oldCount = 0;
            foreach (var key in cdkeys)
            {
                if (_cdKeys.ContainsKey(key))
                {
                    _cdKeys[key].Available = false;
                    oldCount++;
                }
                else
                {
                    _cdKeys.Add(key, new CDKey()
                    {
                        Key = key,
                        Available = false,
                        Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    });
                    oldCount++;
                }
            }

            return (true, $"本次共设置{oldCount}条过期密令。");
        }
        public (bool result, string? desc) UploadCDKey()
        {
            try
            {
                var txt = ConvertCDKeysToString(_cdKeys);
                var ret = _oss.PutData(OSSHelper.PATH_FF, Encoding.UTF8.GetBytes(txt));
                return (ret, ret ? "上传密令成功！" : "上传密令失败！");
            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, $"HandbookM.UploadCDKey");
                return (false, $"上传密令过程中出现错误！详细信息如下\n{ex.Message}");
            }
        }
        /// <summary>
        /// 从阿里云下载CDKEY
        /// </summary>
        /// <param name="callback"></param>
        public (bool result, string desc) DownloadCDKey()
        {
            try
            {
                var bytes = _oss.GetData(OSSHelper.PATH_FF);
                if (bytes is null)
                {
                    return (false, "服务器尚未配置任何密令！");
                }
                var raw = Encoding.UTF8.GetString(_oss.GetData(OSSHelper.PATH_FF));
                var keys = new Dictionary<string, CDKey>();
                dynamic ja = JArray.Parse(raw);
                foreach (var item in ja)
                {
                    string name = JSONHelper.ParseString(item.name);
                    bool available = JSONHelper.ParseBool(item.available);
                    string date = JSONHelper.ParseString(item.date);
                    keys[name] = new()
                    {
                        Key = name,
                        Available = available,
                        Date = date,
                    };
                }
                int newCount = 0;
                int OldCount = 0;
                foreach (var item in keys)
                {
                    if (item.Value.Available)
                    {
                        if (!_cdKeys.ContainsKey(item.Key))
                        {
                            _cdKeys.Add(item.Key, item.Value);
                            newCount++;
                        }
                        else if (!_cdKeys[item.Key].Available)
                        {
                            _cdKeys[item.Key].Available = true;
                            newCount++;
                        }
                    }
                    else
                    {
                        if (_cdKeys.ContainsKey(item.Key))
                        {
                            _cdKeys[item.Key].Available = false;
                            OldCount++;
                        }
                        else
                        {
                            _cdKeys.Add(item.Key, item.Value);
                            OldCount++;
                        }
                    }
                }


                var desc = new List<string>
                    {
                        $"新增{newCount}条新密令\n"
                    };
                if (OldCount > 0) desc.Add($"增加{newCount}条过期密令");
                return (true, $"本次共{string.Join("，", desc)}。");

            }
            catch (Exception ex)
            {
                Context.Logger.WriteException(ex, $"HandbookM.DownloadCDKey");
                return (false, $"下载密令过程中出现错误！详细信息请见日志。");
            }
        }
        /// <summary>
        /// 查询CDKEY
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string QueryCDKey(int index = 1)
        {
            var availableCount = _cdKeys.Count(a => a.Value.Available);
            if (availableCount == 0) return "";
            var start = (index - 1) * 20;
            var end = Math.Min(index * 20, availableCount);
            if (end <= start) return "";
            return System.String.Join("\n", _cdKeys.Values.Where(a => a.Available).Select(a => a.Key).Reverse().ToArray()[start..end]);
        }
        /// <summary>
        /// 查询CDKEY页数
        /// </summary>
        /// <returns></returns>
        public int QueryCDKeyPages()
        {
            // 用linq统计_keys中Available为true的数量?
            var total = _cdKeys.Count(a => a.Value.Available);
            if (total <= 0) return 0;
            return total / 20 + 1;
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

        private void SyncCDKeys()
        {
            if (DownloadCDKey().result) UploadCDKey();
            var group = _context.ContactsM.FindGroup(_context.WechatM.WechatCFG()?.ManagerGroup ?? "");
            if (group is not null)
            {
                _context.WechatM.SendText("同步密令成功！", group.WXID);
            }
        }
        #endregion

        #region VISITORS
        private void LoadVisitors()
        {
            _visitors = new Dictionary<int, HandBookVisitors>();
            var visitorPath = IOHelper.GetCSVPathByType(Tools.Common.Enums.CSVType.ResCSV, "visitor").path;
            if (System.IO.File.Exists(visitorPath))
            {
                var visitorData = System.IO.File.ReadAllText(visitorPath) ?? "";
                visitorData = visitorData.Replace("\r\n", "\n");
                var lines = visitorData.Split('\n');
                if (lines.Length > 1)
                {
                    foreach (var line in lines.Skip(1))
                    {
                        var v = new HandBookVisitors(line);
                        if (v.ID == 0) continue;
                        _visitors.Add(v.ID, v);
                    }
                }
            }
        }
        /// <summary>
        /// 查询逃犯
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string QueryVisitorEscape(string name)
        {
            var escapes = _visitors.Where(x => x.Value.type == "escapee" && x.Value.IsSelectedName(name)).Select(x => x.Key).ToList();
            if (escapes.Count == 0) return $"没有找到与[{name}]相关的逃犯！";
            if (escapes.Count == 1) return $"查询结果\n{_visitors[escapes.First()].GetDescEscape()}";

            var desc = new List<string>();
            var cnt = 0;
            int markID = 0;
            foreach (var item in escapes)
            {
                if (cnt <= 10)
                {
                    desc.Add(_visitors[item].name);
                }
                if (_visitors[item].name == name) markID = item;
                cnt++;
            }
            if (cnt > 10)
            {
                desc.Add("...");
            }
            if (markID > 0)
            {
                desc.Remove(name);
                return $"查询结果\n{_visitors[markID].GetDescEscape()}\n\n" +
                       $"共查找到{escapes.Count}个与[{name}]相关的逃犯：\n" +
                       $"{string.Join("\n", desc.ToArray())}\n" +
                       $"请发送其他名称例如“逃犯 {desc.First()}”进行查找。";
            }
            else
            {
                return $"共查找到{escapes.Count}个与[{name}]相关的逃犯：\n" +
                       $"{string.Join("\n", desc.ToArray())}\n" +
                       $"请发送更具体的名称例如“逃犯 {desc.First()}”进行查找。";
            }
        }
        #endregion

        #region COLLECTIONS
        private object _collectionSearchLock = new object();
        // 今日贵重
        // 搜索贵重
        public List<string> QueryTodayCollectionHandbook()
        {
            lock (_collectionSearchLock)
            {
                List<string>? ids = null;
                try
                {
                    ids = _context.SnailsM.ClientContext.CollectionSerachM.SearchTodayIDs();
                }
                catch (Exception ex)
                {
                    Context.Logger.WriteException(ex, $"HandbookM.QueryTodayCollectionHandbook.SearchIDs");
                }
                var ret = new List<string>();
                if (ids is null || ids.Count == 0) return ret;
                foreach (var id in ids)
                {
                    var path = "";
                    try
                    {
                        path = $"OUT\\COLLECTION_HANDBOOK";
                        if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
                        path = $"{path}\\{id}.png";
                        if (!System.IO.File.Exists(path))
                        {
                            var data = _context.SnailsM.ClientContext.CollectionSerachM.GetContentDataForHandbookThum(id);
                            System.Drawing.Bitmap img = utils.HandbookDrawing.GetCollectionHandbookThum(data);
                            if (img is not null)
                            {
                                img.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Context.Logger.WriteException(ex, $"HandbookM.QueryCollectionHandbook.SearchIDs");
                        path = "";
                    }
                    if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path)) ret.Add(path);
                }
                return ret;
            }
        }
        /// <summary>
        /// 搜索贵重
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public (string path, string desc) QueryCollectionHandbook(string str)
        {
            lock (_collectionSearchLock)
            {
                var searchVal = GetCollectionSearchFilter(str);
                List<string>? ids = null;
                try
                {
                    // _context.SnailsM.ClientContext.CollectionSerachM.ClearCache();

                    ids = _context.SnailsM.ClientContext.CollectionSerachM.SearchIDs(searchVal.searchStr,
                                                                                         searchVal.quality,
                                                                                         searchVal.type,
                                                                                         searchVal.light,
                                                                                         searchVal.enchase,
                                                                                         searchVal.getway,
                                                                                         Client.core.game.include.CollectionSelectStat.ALL);
                }
                catch (Exception ex)
                {
                    Context.Logger.WriteException(ex, $"HandbookM.QueryCollectionHandbook.SearchIDs");
                }
                var desc = "";
                var path = "";

                var names = (ids is not null && ids.Count > 0) ? ids.ToDictionary(id => id, id => _context.SnailsM.ClientContext.CollectionM.QueryName(id)) : new();
                string selectedId = "";
                if (ids is null || ids.Count == 0)
                {
                    desc = "没有找到相关贵重品!";
                }
                //else if (ids.Count > 30)
                //{
                //    desc = $"共找到 {ids.Count} 个相关贵重品:\n{string.Join("\n", ids.Take(30).Select(id => _context.SnailsM.ClientContext.CollectionM.QueryName(id)))}\n...\n请输入更精确的搜索条件。";
                //}
                else if (ids.Count > 1)
                {
                    foreach (var name in searchVal.searchStr.Split(" "))
                    {
                        foreach (var item in names)
                        {
                            if (item.Value == name)
                            {
                                selectedId = item.Key;
                                break;
                            }
                        }
                        if (!string.IsNullOrEmpty(selectedId)) break;
                    }
                    var descNames = names.Values.ToList();
                    if (descNames.Count > 30)
                    {
                        descNames = descNames.Take(30).ToList();
                        descNames.Add("...");
                    }
                    desc = $"共找到 {ids.Count} 个相关贵重品:\n{string.Join("\n", descNames)}\n请按名称查询具体贵重，例如\"贵重 {descNames.First()}\"'。";
                }
                else
                {
                    selectedId = ids.First();
                }

                if (!string.IsNullOrEmpty(selectedId))
                {
                    try
                    {
                        path = $"OUT\\COLLECTION_HANDBOOK";
                        if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
                        path = $"{path}\\{selectedId}.png";
                        if (!System.IO.File.Exists(path))
                        {
                            var data = _context.SnailsM.ClientContext.CollectionSerachM.GetContentDataForHandbookThum(selectedId);
                            System.Drawing.Bitmap img = utils.HandbookDrawing.GetCollectionHandbookThum(data);
                            if (img is not null)
                            {
                                img.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Context.Logger.WriteException(ex, $"HandbookM.QueryCollectionHandbook.SearchIDs");
                        path = "";
                    }
                    if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) desc = "贵重图鉴生成失败了，具体原因请见日志。";
                }


                return (path, desc);
            }
        }

        private Dictionary<string, RS.Snail.JJJ.Client.core.game.include.CollectionSelectQuality> _collectionQualityMap = new()
        {
            {"绿色", RS.Snail.JJJ.Client.core.game.include.CollectionSelectQuality.GREEN},
            {"蓝色", RS.Snail.JJJ.Client.core.game.include.CollectionSelectQuality.BLUE},
            {"紫色", RS.Snail.JJJ.Client.core.game.include.CollectionSelectQuality.PURPLE},
            {"A", RS.Snail.JJJ.Client.core.game.include.CollectionSelectQuality.A},
            {"AA", RS.Snail.JJJ.Client.core.game.include.CollectionSelectQuality.AA},
            {"AAA", RS.Snail.JJJ.Client.core.game.include.CollectionSelectQuality.AAA},
            {"橙色", RS.Snail.JJJ.Client.core.game.include.CollectionSelectQuality.ORANGE},
            {"S", RS.Snail.JJJ.Client.core.game.include.CollectionSelectQuality.S},
            {"SS", RS.Snail.JJJ.Client.core.game.include.CollectionSelectQuality.SS},
            {"SSS", RS.Snail.JJJ.Client.core.game.include.CollectionSelectQuality.SSS},
        };
        private Dictionary<string, RS.Snail.JJJ.Client.core.game.include.CollectionType> _collectionTypeMap = new()
        {
            {"艺术", Client.core.game.include.CollectionType.Art},
            {"科技", Client.core.game.include.CollectionType.Technology},
            {"人气", Client.core.game.include.CollectionType.Influence},
            {"信仰", Client.core.game.include.CollectionType.Religion},
            {"文化", Client.core.game.include.CollectionType.Culture},
        };
        private Dictionary<string, RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight> _collectionLightMap = new()
        {
            {"本体生命",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_SNAIL_HP},
            {"本体攻击",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_SNAIL_ATTACK},
            {"本体防御",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_SNAIL_DEF},
            {"本体追击",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_SNAIL_COMBO},
            {"元素",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_SNAIL_ELEMENT},
            {"领导力",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_SNAIL_LEADERSHIP},
            {"木材",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.RPOP_PROFIT_WOOD},
            {"石材",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.RPOP_PROFIT_ST0NE},
            {"时光机",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.RPOP_PROFIT_TIMEMACHINE},
            {"蝌蚪池",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.RPOP_PROFIT_TODPOLE},
            {"蘑菇园",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.RPOP_PROFIT_MUSHROOM},
            {"巨龙研究",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.RPOP_BODY_DRAGON},
            {"天使研究",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.RPOP_BODY_ANGEL},
            {"恶魔研究",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.RPOP_BODY_DEVIL},
            {"活尸研究",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.RPOP_BODY_ZOMBIE},
            {"异种研究",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.RPOP_BODY_ALIIEN},
            {"机械研究",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.RPOP_BODY_MACHINE},
            {"其他研究",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.RPOP_BODY_ALL},
            {"兵种攻击",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_SON_ATTACK},
            {"兵种防御",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_SON_DEF},
            {"兵种生命",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_SON_HP},
            {"兵种追击",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_SON_COMBO},
            {"所有兵种",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_SON_ALL},
            {"巨龙兵种",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_SON_DRAGON},
            {"天使兵种",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_SON_ANGEL},
            {"恶魔兵种",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_SON_DEVIL},
            {"活尸兵种",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_SON_ZOMBIE},
            {"异种兵种",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_SON_ALIEN},
            {"机械兵种",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_SON_MACHINE},
            {"军团战重伤率",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_DEC_DEATH_RATE},
            {"供奉",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_OTHER_SAC},
            {"探索",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_OTHER_EXPLORE},
            {"杀手",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_OTHER_KILLER},
            {"许愿",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_OTHER_WISH},
            {"其他",RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_OTHER_ALL},
        };
        private Dictionary<string, int> _collectionEnchaseMap = new()
        {
            {"蝌蚪池",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_MONEY_RESOURCE},
            {"时光机",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_STATUE_RESOURCE},
            {"蘑菇园",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_MUSHROOM_RESOURCE},
            {"木材工坊",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_WOOD_RESOURCE},
            {"石材工坊",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_STONE_RESOURCE},

            {"探索罗盘",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_COMPASS},
            {"格斗场",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_ARENA},
            {"基因模拟",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_TOWER},
            {"兵种演练",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_SON_TOWER},
            {"量子增幅器",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_NEBULA_AMPLIFIER},

            {"变异体",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_GROUP_WAR_BUFF},
            {"蜣螂变异体",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_GROUP_WAR_BUFF_TYPE_1},
            {"金鱼变异体",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_GROUP_WAR_BUFF_TYPE_2},
            {"北极贝变异体",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_GROUP_WAR_BUFF_TYPE_3},
            {"仓鼠变异体",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_GROUP_WAR_BUFF_TYPE_4},
            {"螳螂变异体",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_GROUP_WAR_BUFF_TYPE_5},

            {"高丽博物馆",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_MUSEUM1},
            {"扶桑博物馆",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_MUSEUM2},
            {"华夏博物馆",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_MUSEUM3},
            {"米国博物馆",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_MUSEUM4},
            {"英伦博物馆",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_MUSEUM5},
            {"埃罗博物馆",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_MUSEUM6},
            {"希域博物馆",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_MUSEUM7},
            {"天竺博物馆",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_MUSEUM8},
            {"时空博物馆",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_MUSEUM9},

            {"清剿反叛势力",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_RACE_TOWER},
            {"幸运之卵",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_RACE_BONUS1},
            {"许愿之卵",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_RACE_BONUS2},
            {"探索之卵",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_RACE_BONUS3},
            {"供奉之卵",(int)RS.Snail.JJJ.Client.core.game.include.equip.PAGE_RACE_BONUS4},
        };
        private Dictionary<string, RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway> _collectionGetwayMap = new()
        {
            {"1号抽奖机",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.LOTTERY_1},
            {"2号抽奖机",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.LOTTERY_2},
            {"3号抽奖机",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.LOTTERY_3},
            {"八卦炉",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.EIGHT_1},
            {"至尊贵重品",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.VIP_ZUN_XIANG},
            {"许愿贵重品卡册",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.VIP_WISH},
            {"抽奖贵重品卡册",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.VIP_LOTTERY},
            {"纪念章商店",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.VIP_MEDAL},
            {"每日贵重品卡册Ⅰ",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.VIP_DAY},
            {"每日贵重品卡册Ⅱ",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.VIP_DAY_SILVER},
            {"每日贵重品卡册Ⅲ",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.VIP_DAY_GOLD},
            {"文明构成器",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.CIVILIZTION},
            {"火箭科技",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.ROCKET},
            {"NPC来访",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.NPC},
            {"藏宝图",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.TREASURE},
            {"手札",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.LETTER},
            {"宝箱",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.BOX},
            {"基因研究",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.RESEARCH},
            {"礼包",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.GIFT},
            {"彩蛋",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.EGG},
            {"伙伴宿舍",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.PARTNEREGG},
            {"佛龛击落",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.SHRINEFALL},
            {"肖像评价",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.PORTRAIT},
            {"其他",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.OTHER},
            {"未知",RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.UNKNOW},
        };
        private (bool result, RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight light) GetSearchLight(string str)
        {
            var key = str;
            if (key.Contains("光环")) key = key.Replace("光环", "");
            var light = RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight.PROP_ALL;
            foreach (var item in _collectionLightMap)
            {
                if (item.Key.Contains(key))
                {
                    light = item.Value;
                    break;
                }
            }
            return (light != Client.core.game.include.CollectionSelectLight.PROP_ALL, light);
        }
        private (bool result, int enchase) GetSearchEnchase(string str)
        {
            var key = str;
            if (key.Contains("镶嵌")) key = key.Replace("镶嵌", "");
            var enchase = 0;
            foreach (var item in _collectionEnchaseMap)
            {
                if (item.Key.Contains(key))
                {
                    enchase = item.Value;
                    break;
                }
            }
            return (enchase != 0, enchase);
        }
        private (bool result, RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway getway) GetSearchGetway(string str)
        {
            var key = str;
            if (key.Contains("来源")) key = key.Replace("来源", "");
            var getway = RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.ALL;
            foreach (var item in _collectionGetwayMap)
            {
                if (item.Key.Contains(key))
                {
                    getway = item.Value;
                    break;
                }
            }
            return (getway != RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway.ALL, getway);
        }
        /// <summary>
        /// 处理搜索文本，关键词用空格隔开
        /// </summary>
        /// <param name="str"></param>
        /// <returns>(搜索品质，搜索类型，搜索光环，搜索镶嵌，搜索获取途径，搜索镶嵌，搜索字符串)</returns>
        public (RS.Snail.JJJ.Client.core.game.include.CollectionSelectQuality quality,
                RS.Snail.JJJ.Client.core.game.include.CollectionType type,
                RS.Snail.JJJ.Client.core.game.include.CollectionSelectLight light,
                RS.Snail.JJJ.Client.core.game.include.CollectionSelectGetway getway,
                int enchase,
                string searchStr) GetCollectionSearchFilter(string str)
        {
            // 贵重 橙色 人气 
            var quality = Client.core.game.include.CollectionSelectQuality.ALL;
            var type = Client.core.game.include.CollectionType.ALL;
            var light = Client.core.game.include.CollectionSelectLight.PROP_ALL;
            var getway = Client.core.game.include.CollectionSelectGetway.ALL;
            int enchase = 0;

            var arr = str.Replace("光环 ", "光环").Replace("镶嵌 ", "镶嵌").Replace("ID ", "ID").Split(" ").ToList();
            var removes = new List<string>();
            foreach (var key in arr)
            {
                var approved = false;
                if (_collectionQualityMap.ContainsKey(key.ToUpper()))
                {
                    approved = true;
                    if (quality == Client.core.game.include.CollectionSelectQuality.ALL) quality = _collectionQualityMap[key.ToUpper()];
                }
                else if (_collectionTypeMap.ContainsKey(key))
                {
                    approved = true;
                    if (type == Client.core.game.include.CollectionType.ALL) type = _collectionTypeMap[key];
                }
                else if (key.StartsWith("光环"))
                {
                    approved = true;
                    var lightVal = GetSearchLight(key);
                    if (lightVal.result && light == Client.core.game.include.CollectionSelectLight.PROP_ALL) light = lightVal.light;
                }
                else if (key.StartsWith("镶嵌"))
                {
                    approved = true;
                    var enchaseVal = GetSearchEnchase(key);
                    if (enchaseVal.result && enchase == 0) enchase = enchaseVal.enchase;
                }
                else if (key.StartsWith("来源"))
                {
                    var getwayVal = GetSearchGetway(key);
                    if (getwayVal.result && getway == Client.core.game.include.CollectionSelectGetway.ALL) getway = getwayVal.getway;
                }

                if (approved) removes.Add(key);
            }

            arr = arr.Where(x => !removes.Contains(x)).ToList();
            return (quality, type, light, getway, enchase, string.Join(" ", arr));
        }

        #endregion

        #region CD
        private static string GetHandbookCDKey(string wxid) => $"handbook_{wxid}";
        public bool CheckCD(string wxid)
        {
            if (_context.ContactsM.IsAdmin(wxid)) return true;
            var key = GetHandbookCDKey(wxid);
            var lastTime = _context.CdM.GetCache(key);
            var now = TimeHelper.ToTimeStamp();
            if (lastTime + _context.ConfigsM.HandbookInterval >= now) return false;
            _context.CdM.SetCache(key, now);
            return true;
        }
        #endregion

        #region CLASS
        internal class CDKey
        {
            public string Key { get; set; }
            public bool Available { get; set; }
            public string Date { get; set; }
        }
        internal class HandBookVisitors
        {
            //ID,类型,名称,简介,等级,奖励次数,复仇概率,复仇奖励,抓住给黑蝌蚪,感谢概率,感谢限定奖励,感谢奖励,奖励,花费,战斗ID,敌人名称,类型,HP,ATK,COM,DEF,WEAK,BONUS,
            public int ID { get; }
            public string type { get; }
            public string name { get; }
            public string desc { get; }
            public int level { get; }
            public int revengeTime { get; }
            public int revengeRate { get; }
            public string revengeBonus { get; }
            public int rewardMoney { get; }
            public int rewardRate { get; }
            public string rewardBonusLimit { get; }
            public string rewardBonus { get; }
            public string bonus { get; }
            public string cost { get; }
            public int combatID { get; }
            public string combatName { get; }
            public string combatType { get; }
            public int combatHP { get; }
            public int combatAtk { get; }
            public int combatCom { get; }
            public int combatDef { get; }
            public string combatWeak { get; }
            public string combatProp { get; }
            public HandBookVisitors(string src)
            {
                if (string.IsNullOrEmpty(src)) return;
                src = src.Replace("; ", ";").Replace(";", "\n");
                var arr = src.Split(",");
                if (arr.Length < 23) return;
                ID = Convert.ToInt32(arr[0]);
                type = arr[1];
                name = arr[2];
                desc = arr[3];
                level = Convert.ToInt32(arr[4] == "" ? null : arr[4]);
                revengeTime = Convert.ToInt32(arr[5] == "" ? null : arr[5]);
                revengeRate = Convert.ToInt32(arr[6] == "" ? null : arr[6]);
                revengeBonus = arr[7];
                rewardMoney = Convert.ToInt32(arr[8] == "" ? null : arr[8]);
                rewardRate = Convert.ToInt32(arr[9] == "" ? null : arr[9]);
                rewardBonusLimit = arr[10];
                rewardBonus = arr[11];
                bonus = arr[12];
                cost = arr[13];
                combatID = Convert.ToInt32(arr[14] == "" ? null : arr[14]);
                combatName = arr[15];
                combatType = arr[16];
                combatHP = Convert.ToInt32(arr[17] == "" ? null : arr[17]);
                combatAtk = Convert.ToInt32(arr[18] == "" ? null : arr[18]);
                combatCom = Convert.ToInt32(arr[19] == "" ? null : arr[19]);
                combatDef = Convert.ToInt32(arr[20] == "" ? null : arr[20]);
                combatWeak = arr[21];
                combatProp = arr[22];
            }
            private string EscapeLevel()
            {
                switch (level)
                {
                    case 1: return "初级";
                    case 2: return "中级";
                    case 3: return "高级";
                    case 4: return "超级";
                    case 5: return "星系级";
                    case 6: return "宇宙级";
                    case 7: return "次元级";
                    case 8: return "全能级";
                    default: return "未知等级";
                }
            }
            public string GetDescEscape()
            {
                var list = new List<string>();
                list.Add($"[{name}] {EscapeLevel()}");

                if (!string.IsNullOrEmpty(revengeBonus))
                {
                    list.Add("\n👉BA(复仇) (限1次)");
                    list.Add("奖励: " + revengeBonus);
                    list.Add($"需击败 [{combatName}]:\n血{combatHP}/攻{combatAtk}/追{combatCom}/防{combatDef}\n{combatWeak}" +
                        (string.IsNullOrEmpty(combatProp) ? "" : $"\n{combatProp}"));
                }

                list.Add("\n👉BA(抓住)");
                list.Add($"奖励: 黑蝌蚪{rewardMoney}");

                if (!string.IsNullOrEmpty(rewardBonusLimit) || !string.IsNullOrEmpty(rewardBonus))
                {
                    list.Add("\n👉BB(感谢)");
                    if (!string.IsNullOrEmpty(rewardBonus.Trim()))
                    {
                        list.Add("奖励(限1次):" + rewardBonus);
                    }
                    else if (!string.IsNullOrEmpty(rewardBonusLimit))
                    {
                        list.Add("奖励(限1次): " + rewardBonusLimit);
                    }
                }
                return string.Join("\n", list.ToArray());
            }
            public string GetDescLegacy()
            {
                return $"👉{bonus}(需花费黑蝌蚪{cost.Split("+").Last()})";
            }
            public bool IsSelectedName(string src)
            {
                if (name.Contains(src)) return true;
                var str = Pinyin.GetPinyin(name);
                src = src.ToLower();
                if (str.Contains(src)) return true;
                str = Pinyin.GetInitials(name);
                if (str.Contains(src)) return true;
                return false;
            }
            public bool IsSelectedDesc(string src)
            {
                if (desc.Contains(src)) return true;
                var str = Pinyin.GetPinyin(desc);
                src = src.ToLower();
                if (str.Contains(src)) return true;
                str = Pinyin.GetInitials(desc);
                if (str.Contains(src)) return true;
                return false;
            }

        }
        #endregion
    }
}
