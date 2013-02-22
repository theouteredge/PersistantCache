using System;
using System.Collections.Concurrent;
using System.Runtime.Caching;

namespace PersistentCache
{
    public class BlockingCache
    {
        private readonly ConcurrentDictionary<string, CachedItem> _cache = new ConcurrentDictionary<string, CachedItem>();
        private readonly object _lock = new object();


        public bool Contains(string key)
        {
            return _cache.ContainsKey(key);
        }

        public object this[string key]
        {
            get { return _cache[key].Value; }
        }
        

        public bool Set(string key, object value, Action<CacheEntryRemovedArguments> removedCallback)
        {
            return Contains(key) ? Put(key, value) : Add(key, new CachedItem(key, value, removedCallback));
        }

        private bool Add(string key, CachedItem value)
        {
            try
            {
                return _cache.TryAdd(key, value);
            }
            catch (Exception) // check for the actual Exception we should be catching
            {
                // cheat, if the key already exists, someone else got there first, so just say we added it
                return _cache.ContainsKey(key);
            }
        }

        private bool Put(string key, object value)
        {
            //lock (_lock)
            //{
                var item = _cache[key];
                if (item == null)
                    return false;

                item.Value = value;
            //}

            return true;
        }
    }

    internal class CachedItem
    {
        internal string Key { get; set; }

        private object _value;
        internal object Value
        {
            get
            {
                AccessCount += 1;
                LastAccess = DateTime.Now;

                return _value;
            }
            set
            {
                _value = value;

                AccessCount += 1;
                LastAccess = DateTime.Now;
            }
        }

        internal int AccessCount { get; private set; }
        internal DateTime LastAccess { get; private set; }

        internal Action<CacheEntryRemovedArguments> RemovedCallback { get; set; }


        internal CachedItem(string key, object value, Action<CacheEntryRemovedArguments> removedCallback)
        {
            Key = key;
            Value = value;
            RemovedCallback = removedCallback;

            AccessCount = 0;
            LastAccess = DateTime.Now;
        }
    }
}