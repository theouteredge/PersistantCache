using System;
using System.Collections.Generic;

namespace PersistentCache
{
    public interface ICacheToDisk : IDisposable
    {
        bool Contains(string key);

        void Put(string key, object value);

        TValue Get<TValue>(string key);
        bool TryGet<TValue>(string key, out TValue value);

        IEnumerable<KeyValuePair<string, TValue>> GetEnumerable<TValue>();
    }
}