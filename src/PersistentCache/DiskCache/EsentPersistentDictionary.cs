using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Collections.Generic;
using ServiceStack.Text;

namespace PersistentCache.DiskCache
{
    public class EsentPersistentDictionary : ICacheToDisk
    {
        private PersistentDictionary<string, string> _persistentDictionary;
        private bool _disposing = false;
        private string _path;


        public EsentPersistentDictionary(string path)
        {
            _persistentDictionary = new PersistentDictionary<string, string>(path);
            _path = path;
        }



        public void Dispose()
        {
            if (_persistentDictionary != null && !_disposing)
            {
                _disposing = true;

                _persistentDictionary.Dispose();
                _persistentDictionary = null;

                try
                {
                    if (Directory.Exists(_path))
                        Directory.Delete(_path, true);
                }
                catch (Exception)
                {
                    // swallow the error that we couldn't clean the directory for now.
                }
            }
        }


        public bool Contains(string key)
        {
            return _persistentDictionary != null && _persistentDictionary.ContainsKey(key);
        }

        public void Put(string key, object value)
        {
            if (_persistentDictionary != null && !_disposing)
                _persistentDictionary.Add(key, value.ToJson());
        }

        public TValue Get<TValue>(string key)
        {
            if (_persistentDictionary != null && !_disposing)
                return _persistentDictionary[key].FromJson<TValue>();

            return default(TValue);
        }

        public bool TryGet<TValue>(string key, out TValue value)
        {
            if (_persistentDictionary != null && !_disposing)
            {
                if (Contains(key))
                {
                    value = _persistentDictionary[key].FromJson<TValue>();
                    return true;
                }
            }

            value = default(TValue);
            return false;
        }
    }
}
