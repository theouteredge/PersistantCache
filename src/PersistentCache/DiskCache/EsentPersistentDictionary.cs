using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Collections.Generic;
using ServiceStack;
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

                var databasePath = _persistentDictionary.Database;

                _persistentDictionary.Dispose();
                _persistentDictionary = null;

                PersistentDictionaryFile.DeleteFiles(databasePath);

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

            // force the Esent store to be cleaned out of memory
            GC.Collect(2, GCCollectionMode.Forced);
        }



        public IEnumerable<KeyValuePair<string, object>> GetEnumerable<TValue>()
        {
            return _persistentDictionary.Select(x => new KeyValuePair<string, object>(x.Key, x.Value.FromJson<TValue>()));
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
