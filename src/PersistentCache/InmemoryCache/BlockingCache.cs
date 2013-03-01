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
        private readonly TimeSpan _maintenanceSchedule;
        private readonly TimeSpan _unusedItemLimit;
        private readonly int _trimThreshold;

        private readonly Timer _timer;


        public Action<string, object> CacheItemRemovedCallback { get; private set; }


        public BlockingCache(int maxCacheSize, TimeSpan maintenanceSchedule, TimeSpan unusedItemLimit, Action<string, object> cacheItemRemovedCallback)
        {
            CacheItemRemovedCallback = cacheItemRemovedCallback;
            _maxCacheSize = maxCacheSize;
            _maintenanceSchedule = maintenanceSchedule;
            _unusedItemLimit = unusedItemLimit;
            _trimThreshold = Convert.ToInt32(maxCacheSize * 0.9);

            _timer = new Timer(ManageCacheSize, null, maintenanceSchedule, maintenanceSchedule);
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

        public bool TryRemove(string key, out object value)
        {
            CachedValue item;
            lock (_writelock)
            {
                if (_cache != null)
                {
                    item = _cache[key];
                    _cache.Remove(key);
                }
                else
                {
                    value = null;
                    return false;
                }
            }

            value = item.Value;
            return true;
        }


        private void ManageCacheSize(object state)
        {
            if (_cache == null)
                return;

            // trim all the cache items which have never been hit, and have been hanging around for 30seconds
            if (_cache.Count >= _trimThreshold)
            {
                KeyValuePair<string, CachedValue>[] cacheItems;

                lock (_writelock)
                    cacheItems = _cache.Where(x => x.Value.HitCount == 1 && (DateTime.Now - x.Value.LastHitAt).TotalSeconds >= _unusedItemLimit.TotalSeconds)
                                       .Select(x => new KeyValuePair<string, CachedValue>(x.Key, x.Value))
                                       .ToArray();

                foreach (var item in cacheItems)
                    RemoveItemFromCache(item);
            }

            // ok, we are still over, lets remove all items which haven't been accessed in over 30seconds
            if (_cache.Count >= _trimThreshold)
            {
                KeyValuePair<string, CachedValue>[] cacheItems;

                lock (_writelock)
                    cacheItems = _cache.Where(x => (DateTime.Now - x.Value.LastHitAt).TotalSeconds >= _unusedItemLimit.TotalSeconds)
                                       .Select(x => new KeyValuePair<string, CachedValue>(x.Key, x.Value))
                                       .ToArray();

                foreach (var item in cacheItems)
                    RemoveItemFromCache(item);
            }

            // well fuck, order by the least hit and drop back down to the trimThreashold
            if (_cache.Count >= _maxCacheSize)
            {
                var target = _cache.Count - _trimThreshold;

                KeyValuePair<string, CachedValue>[] cacheItems;
                lock (_writelock)
                    cacheItems = _cache.OrderBy(x => x.Value.HitCount)
                                       .Take(target)
                                       .ToArray();

                foreach (var item in cacheItems)
                    RemoveItemFromCache(item);
            }
        }

        private void RemoveItemFromCache(KeyValuePair<string, CachedValue> item)
        {
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
