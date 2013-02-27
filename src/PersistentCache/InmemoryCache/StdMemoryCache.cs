using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace PersistentCache.InmemoryCache
{
    public class StdMemoryCache : ICache
    {
        private MemoryCache _cache;


        public Action<string, object> CacheItemRemovedCallback { get; private set; }


        public StdMemoryCache(NameValueCollection config, Action<string, object> cacheItemRemovedCallback)
        {
            _cache = new MemoryCache("MainCache", config);
            CacheItemRemovedCallback = cacheItemRemovedCallback;
        }

        public void Dispose()
        {
            _cache = null;
        }

        

        public bool TryGet(string key, out object value)
        {
            var tmp = _cache.Get(key);
            if (tmp == null)
            {
                value = null;
                return false;
            }

            value = tmp;
            return true;
        }

        public bool TryAdd(string key, object value)
        {
            return _cache.Add(key, value, new CacheItemPolicy() {RemovedCallback = RemovedCallback});
        }

        public bool TryRemove(string key, out object value)
        {
            value = _cache.Remove(key);
            return value != null;
        }



        private void RemovedCallback(CacheEntryRemovedArguments arguments)
        {
            // if we are not removing the item cos it was removed by the caller then save it to disk
            if (arguments.RemovedReason == CacheEntryRemovedReason.Evicted || arguments.RemovedReason == CacheEntryRemovedReason.Expired)
            {
                if (CacheItemRemovedCallback != null)
                    CacheItemRemovedCallback.Invoke(arguments.CacheItem.Key, arguments.CacheItem.Value);
           }
        }
    }
}
