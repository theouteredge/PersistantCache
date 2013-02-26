using System;

namespace PersistentCache.Cache
{
    public interface ICache
    {
        Action<string, object> CacheItemRemovedCallback { get; }

        bool TryGet(string key, out object value);
        bool TryAdd(string key, object value);
    }
}