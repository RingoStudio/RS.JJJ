using Newtonsoft.Json.Linq;
using RS.Snail.JJJ.boot;
using RS.Snail.JJJ.Client.core.game.bas;
using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.clone
{
    internal class Dbase
    {
        private Context _context;
        public dynamic dbase { get; set; }
        public dynamic temp_dbase { get; set; } = new JObject();
        public dynamic cb { get; set; } = new JObject();
        public bool IsEmpty { get => dbase is null || JSONHelper.GetCount(dbase) <= 0; }
        public bool IsCbEmpty { get => cb is null || JSONHelper.GetCount(cb) <= 0; }
        public Dbase(Context context, dynamic? dbase)
        {
            _context = context;
            if (dbase is not JObject)
            {
                this.dbase = new JObject();
                return;
            }
            dbase ??= new JObject();
            if (dbase is not JObject) dbase = JObject.FromObject(dbase);
            this.dbase = dbase;
            //this.cb = new JObject();
        }
        public Dbase Clone()
        {
            return new Dbase(_context, this.dbase.DeepClone());
        }
        public Dbase(Context context)
        {
            _context = context;
            this.dbase = new JObject();
            //this.cb = new JObject();
        }
        public Dbase __add(Dbase a, Dbase b)
        {
            var r = new Dbase(_context);
            if (a is not null && !a.IsEmpty)
            {
                foreach (var item in a.dbase)
                {
                    r.dbase[item.Name] = item.Value;
                }
            }
            if (b is not null && !b.IsEmpty)
            {
                foreach (var item in b.dbase)
                {
                    r.dbase[item.Name] = item.Value;
                }
            }
            if (a is not null && !a.IsCbEmpty)
            {
                foreach (var item in a.cb)
                {
                    r.cb[item.Name] = item.Value;
                }
            }
            if (b is not null && !b.IsCbEmpty)
            {
                foreach (var item in b.cb)
                {
                    r.cb[item.Name] = item.Value;
                }
            }
            return r;
        }

        #region DBASE
        public void Replace(dynamic data) => this.dbase = data;
        public void ReplaceTemp(dynamic data) => this.temp_dbase = data;
        public bool Absorb(dynamic data)
        {
            dbase ??= new JObject();
            foreach (var item in data)
            {
                this.dbase[item.Name] = item.Value;
                TriggerField(item.Name);
            }
            return true;
        }
        public bool Set(string path, dynamic k, dynamic v = null)
        {
            dbase ??= new JObject();
            try
            {
                bool flag;
                if (v is not null && k is string && v is not null)
                {
                    var newPath = $"{path}/{k}";
                    flag = ExpressMapping.ExpressSet(newPath, dbase, v);
                }
                else
                {
                    flag = ExpressMapping.ExpressSet(path, dbase, k);
                }
                if (flag)
                {
                    TriggerField(path);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool SetEx(string path, dynamic v)
        {
            dbase ??= new JObject();
            if (ExpressMapping.ExpressSet(path, dbase, v))
            {
                TriggerField(path);
                return true;
            }
            return false;
        }
        public dynamic? Query(string path, dynamic? path2 = null, dynamic? defaults = null)
        {
            dbase ??= new JObject();
            if (defaults is not null)
            {
                var newPath = path;
                if (!string.IsNullOrEmpty(path2 as string)) newPath = $"{newPath}/{path2}";
                return ExpressMapping.ExpressQuery(newPath, dbase, defaults);
            }
            else
            {
                return ExpressMapping.ExpressQuery(path, dbase, path2);
            }
        }
        public dynamic? QueryEx(string path, dynamic? defaults = null)
        {
            dbase ??= new JObject();
            return ExpressMapping.ExpressQuery(path, dbase, defaults);
        }

        public bool Delete(string path, string path2 = "")
        {
            if (string.IsNullOrEmpty(path))
            {
                dbase = new JObject();
                return true;
            }
            else
            {
                dbase ??= new JObject();
                var newPath = path;
                if (!string.IsNullOrEmpty(path2)) newPath = $"{newPath}/{path2}";
                if (ExpressMapping.ExpressDelete(newPath, dbase))
                {
                    TriggerField(path);
                    return true;
                }
                return false;
            }
        }
        public bool DeleteEx(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                dbase = new JObject();
                return true;
            }
            else
            {
                dbase ??= new JObject();
                if (ExpressMapping.ExpressDelete(path, dbase))
                {
                    TriggerField(path);
                    return true;
                }
                return false;
            }
        }
        #endregion

        #region TEMP DBASE
        public bool SetTemp(string path, dynamic k, dynamic v = null)
        {
            try
            {
                bool flag;
                if (v is not null && k is string)
                {
                    var newPath = $"{path}/{k}";
                    flag = ExpressMapping.ExpressSet(newPath, temp_dbase, v);
                }
                else
                {
                    flag = ExpressMapping.ExpressSet(path, temp_dbase, k);
                }
                if (flag)
                {
                    TriggerField(path);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool SetTempEx(string path, dynamic v)
        {
            if (ExpressMapping.ExpressSet(path, temp_dbase, v))
            {
                TriggerField(path);
                return true;
            }
            return false;
        }
        public dynamic QueryTemp(string path, dynamic path2 = null, dynamic defaults = null)
        {
            dynamic result = null;
            if (defaults is not null)
            {
                var newPath = path;
                if (!string.IsNullOrEmpty(path2 as string)) newPath = $"{newPath}/{path2}";
                return ExpressMapping.ExpressQuery(newPath, temp_dbase, defaults);
            }
            else
            {
                return ExpressMapping.ExpressQuery(path, temp_dbase, path2);
            }
        }
        public dynamic QueryTempEx(string path, dynamic defaults = null)
        {
            return ExpressMapping.ExpressQuery(path, temp_dbase, defaults);
        }
        public bool DeleteTemp(string path, string path2 = "")
        {
            if (string.IsNullOrEmpty(path))
            {
                temp_dbase = new JObject();
                return true;
            }
            else
            {
                var newPath = path;
                if (!string.IsNullOrEmpty(path2)) newPath = $"{newPath}/{path2}";
                if (ExpressMapping.ExpressDelete(newPath, temp_dbase))
                {
                    TriggerField(path);
                    return true;
                }
                return false;
            }
        }
        public bool DeleteTempEx(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                temp_dbase = new JObject();
                return true;
            }
            else
            {
                if (ExpressMapping.ExpressDelete(path, temp_dbase))
                {
                    TriggerField(path);
                    return true;
                }
                return false;
            }
        }
        #endregion

        #region CB
        public void RegistrCB(string name, dynamic fields, string f)
        {
            var arr = new List<string>();
            if (fields is List<string>)
            {
                arr = fields;
            }
            else if (fields is string)
            {
                arr.Add(fields);
            }

            foreach (var field in arr)
            {
                if (cb[field] is null) cb[field] = new JObject();
                if (cb[field][name] is not null) continue;
                cb[field][name] = f;
            }
        }
        public void RemoveCB(string name, dynamic fields)
        {
            var arr = new List<string>();
            if (fields is List<string>)
            {
                arr = fields;
            }
            else if (fields is string)
            {
                arr.Add(fields);
            }
            foreach (var field in arr)
            {
                if (cb[field] is not null)
                {
                    cb[field].Remove(name);
                }
            }

        }
        public void TriggerField(string path)
        {

        }
        #endregion
    }
}
