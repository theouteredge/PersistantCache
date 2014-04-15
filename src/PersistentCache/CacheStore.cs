using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PersistentCache.DiskCache;
using PersistentCache.InmemoryCache;
using PersistentCache.Util;

namespace PersistentCache
{
    /// <remarks>
    /// AUTHOR
    /// ------
    /// Andy Long | @theouteredge | andy@theouteredge.co.uk
    /// 
    /// DEPENDANCIES
    /// ------------
    /// ServiceStack.Text
    /// CSharpTest.Net.Library
    /// CSharpTest.Net.BPlusTree
    /// protobuf-net
    /// </remarks>
    /// 
    /// <summary>
    /// A Persistent Cache. Once a key/value is stored in the cache its stored for the lifetime of the cache.
    /// This is designed to be used in a very specific use case where you would want to large cache which would be larger than the available
    /// system memory and you want the cache to store all the values while performing a single long running process. Once the process has completed the
    /// cache should be disposed of.
    /// 
    /// Once the available memory starts to run out and items are evicted or expired from the inmemory cache they will be persisted to the disk based cache.
    /// 
    /// Persistant Cache will first check the InMemory cache before looking on disk for your item
    /// </summary>
    public class CacheStore<T> : IDisposable
    {
        //private readonly MemoryCache _cache;
        private ICache _cache;
        private ICacheToDisk _diskCache;
        private bool _itemsCachedToDisk = false;


        public string PollingInterval { get; private set; }
        public string PhysicalMemoryLimitPercentage { get; private set; }
        public string CacheMemoryLimitMegabytes { get; private set; }

        public string BaseDirectory { get; set; }

        public CacheStore(string baseDirectory)
        {
            BaseDirectory = baseDirectory;
            
            var config = new NameValueCollection
                {
                    {"pollingInterval", "00:01:00" },
                    {"physicalMemoryLimitPercentage", "80" }
                };

            if (!Directory.Exists(baseDirectory))
                Directory.CreateDirectory(BaseDirectory);

            _cache = new StdMemoryCache(config, RemovedCallback);
            _diskCache = new EsentPersistentDictionary(baseDirectory);
        }

        public CacheStore(string baseDirectory, string cacheMemoryLimitMegabytes, string physicalMemoryLimitPercentage, string pollingInterval, ICacheToDisk diskCache)
        {
            BaseDirectory = baseDirectory;
            _diskCache = diskCache;


            var config = new NameValueCollection
                {
                    {"pollingInterval", pollingInterval ?? "00:01:00" },
                    {"physicalMemoryLimitPercentage", physicalMemoryLimitPercentage ?? "0" },
                    {"cacheMemoryLimitMegabytes", cacheMemoryLimitMegabytes ?? "100" }
                };

            if (!Directory.Exists(baseDirectory))
                Directory.CreateDirectory(BaseDirectory);

            _cache = new StdMemoryCache(config, RemovedCallback);
        }



        public void Put(string key, object value, int itemExpiration = 10)
        {
            _cache.TryAdd(Hash(key), value);
        }


        public bool TryGet<TResult>(string key, out TResult value)
        {
            key = Hash(key);

            object valueTmp;
            if (_cache.TryGet(key, out valueTmp))
            {
                value = (TResult)valueTmp;
                return true;
            }

            if (_itemsCachedToDisk)
            {
                // it wasn't in the memory cache & we have items on disk, so look for it within the ICacheToDisk
                TResult item;
                if (_diskCache.TryGet(key, out item))
                {
                    value = item;
                    return true;
                }
            }

            value = default(TResult);
            return false;
        }


        public void Remove(string key)
        {
            object item;
            _cache.TryRemove(Hash(key), out item);
        }


        private void RemovedCallback(string key, object value)
        {
            if (_diskCache != null)
            {
                _diskCache.Put(key, value);
                _itemsCachedToDisk = true;

                if (!Directory.Exists(BaseDirectory))
                    Directory.Delete(BaseDirectory, true);
            }
        }

        
        private static string Hash(string s)
        {
            var hash = new SHA1Managed();
            var hashedBuffer = hash.ComputeHash(Encoding.UTF8.GetBytes(s));

            return hashedBuffer.ToHex();
        }

        public void Dispose()
        {
            if (_diskCache != null)
            {
                _diskCache.Dispose();
                _diskCache = null;
            }
                

            if (_cache != null)
                _cache.Dispose();
        }
    }
}