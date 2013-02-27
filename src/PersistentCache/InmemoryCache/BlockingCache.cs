using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PersistentCache.InmemoryCache
{
    public class BlockingCache : ICache
    {
        // slow 55secs for 2million items
        //private ConcurrentDictionary<string, CachedValue> _cache = new ConcurrentDictionary<string, CachedValue>();

        // 16secs for 2million items
        private Dictionary<string, CachedValue> _cache = new Dictionary<string, CachedValue>();

        // slow 70secs
        //private SortedDictionary<string, CachedValue> _cache = new SortedDictionary<string, CachedValue>();

        
        private readonly int _maxCacheSize;
        private readonly int _trimThreshold;

        private readonly Timer _timer;

        
        public Action<string, object> CacheItemRemovedCallback { get; private set; }


        public BlockingCache(int maxCacheSize, TimeSpan timeSpan, Action<string, object> cacheItemRemovedCallback)
        {
            CacheItemRemovedCallback = cacheItemRemovedCallback;
            _maxCacheSize = maxCacheSize;
            _trimThreshold = Convert.ToInt32(maxCacheSize*0.9);

            _timer = new Timer(ManageCacheSize, null, timeSpan, timeSpan);
        }

        
        public bool TryGet(string key, out object value)
        {
            CachedValue cacheItem;
            var success = _cache.TryGetValue(key, out cacheItem);

            if (success)
            {
                value = cacheItem.Value;
                return true;
            }

            value = null;
            return false;
        }

        readonly object _writelock = new object();
        public bool TryAdd(string key, object value)
        {
            var result = false;
            lock (_writelock)
            {
                if (!_cache.ContainsKey(key))
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


        private void ManageCacheSize(object state)
        {
            if (_cache == null)
                return;

            var limit = TimeSpan.FromSeconds(30);

            // trim all the cache items which have never been hit, and have been hanging around for 30seconds
            if (_cache.Count >= _trimThreshold)
            {
                var cacheItems = _cache.Where(x => x.Value.HitCount == 1);

                foreach (var item in cacheItems)
                {
                    if (item.Value.LastHitAt - DateTime.Now >= limit)
                        RemoveItemFromCache(item);
                }
            }

            // ok, we are still over, lets remove all items which haven't been accessed in over 30seconds
            if (_cache.Count >= _trimThreshold)
            {
                var cacheItems = _cache.Where(x => x.Value.LastHitAt - DateTime.Now >= limit);

                foreach (var item in cacheItems)
                {
                    if (item.Value.LastHitAt - DateTime.Now > limit)
                        RemoveItemFromCache(item);
                }
            }

            // well fuck, order by the least hit and drop back down to the trimThreashold
            if (_cache.Count >= _maxCacheSize)
            {
                var target = _cache.Count - _trimThreshold;
                var count = 0;
                foreach (var item in _cache.OrderBy(x => x.Value.HitCount))
                {
                    RemoveItemFromCache(item);
                    count++;

                    if (count > target)
                        break; // we've trimmed the cache enough so break from the loop before we delete everything
                }
            }
        }

        private void RemoveItemFromCache(KeyValuePair<string, CachedValue> item)
        {
            CacheItemRemovedCallback.Invoke(item.Key, item.Value);

            object value;
            TryRemove(item.Key, out value);
        }

        public void Dispose()
        {
            _cache = null;
            _timer.Dispose();
        }
    }
}
