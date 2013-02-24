using System;

namespace PersistentCache
{
    public interface ICacheToDisk : IDisposable
    {
        bool Contains(string key);

        void Put(string key, object value);

        T Get<T>(string key);
        bool TryGet<T>(string key, out T value);
        string Get(string key);
    }
}