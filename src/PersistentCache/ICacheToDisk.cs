using System;

namespace PersistentCache
{
    public interface ICacheToDisk : IDisposable
    {
        bool Contains(string key);

        void Put(string key, object value);

        TValue Get<TValue>(string key);
        bool TryGet<TValue>(string key, out TValue value);
        //string Get(string key);
    }
}