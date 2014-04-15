using System;
using System.Collections.Generic;

namespace PersistentCache.InmemoryCache
{
    public interface ICache : IDisposable
    {
        Action<string, object> CacheItemRemovedCallback { get; }

        bool TryGet(string key, out object value);
        bool TryAdd(string key, object value);
        bool TryRemove(string key, out object value);

        IEnumerable<KeyValuePair<string, object>> GetEnumerable();
    }
}