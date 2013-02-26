using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PersistentCache.Cache
{
    public class BlockingCache : ICache
    {
        private readonly ConcurrentDictionary<string, CachedValue> _cache = new ConcurrentDictionary<string, CachedValue>();
        
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

        public bool TryAdd(string key, object value)
        {
            var result = _cache.TryAdd(key, new CachedValue(value));
            return result;
        }

        public bool TryRemove(string key, out object value)
        {
            CachedValue item;
            var result = _cache.TryRemove(key, out item);

            value = item.Value;
            return result;
        }


        private void ManageCacheSize(object state)
        {
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

            CachedValue value;
            _cache.TryRemove(item.Key, out value);
        }
    }
}
