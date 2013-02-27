using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C5;

namespace PersistentCache.InmemoryCache
{
    public class RedBlackTreeCache : ICache
    {
        public Action<string, object> CacheItemRemovedCallback { get; private set; }

        private TreeDictionary<string, CachedValue> _cache;


        public RedBlackTreeCache(Action<string, object> callback)
        {
            CacheItemRemovedCallback = callback;
            _cache = new TreeDictionary<string, CachedValue>();
        }


        public bool TryGet(string key, out object value)
        {
            lock (_writelock)
            {
                if (_cache.Contains(key))
                {
                    value = _cache[key].Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        private readonly object _writelock = new object();
        public bool TryAdd(string key, object value)
        {
            var result = false;
            lock (_writelock)
            {
                if (!_cache.Contains(key))
                {
                    _cache.Add(key, new CachedValue(value));
                    result = true;
                }
            }

            return result;
        }


        private readonly object _removelock = new object();
        public bool TryRemove(string key, out object value)
        {
            CachedValue item;
            lock (_removelock)
            {
                item = _cache[key];
                _cache.Remove(key);
            }

            value = item.Value;
            return true;
        }

        public void Dispose()
        {
            _cache = null;
        }
    }
}
