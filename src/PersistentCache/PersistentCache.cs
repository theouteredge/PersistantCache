using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace PersistentCache
{
    public class PersistentCache
    {
        private readonly HashAlgorithm _hash;
        private readonly MemoryCache _cache;

        public string PollingInterval { get; set; }
        public string PhysicalMemoryLimitPercentage { get; set; }
        public string CacheMemoryLimitMegabytes { get; set; }

        public string BaseDirectory { get; set; }



        public PersistentCache(HashAlgorithm hash = null)
        {
            _hash = hash ?? new SHA1Managed();

            var config = new NameValueCollection
                {
                    {"pollingInterval", PollingInterval ?? "00:01:00" },
                    {"physicalMemoryLimitPercentage", PhysicalMemoryLimitPercentage ?? "0" },
                    {"cacheMemoryLimitMegabytes", CacheMemoryLimitMegabytes ?? "100" }
                };

            _cache = new MemoryCache("MainCache", config);
        }


        public void Put(string key, object value)
        {
            _cache.Add(key, value, new CacheItemPolicy() {RemovedCallback = RemovedCallback});
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_cache.Contains(key))
            {
                value = (T)_cache[key];
                return true;
            }

            // it wasn't in the memory cache, so look for a file with the keys name
            var filename = GetSafeFileName(key);
            if (File.Exists(filename))
            {
                value = File.ReadAllText(BaseDirectory + filename).FromJson<T>();
                return true;
            }

            value = default(T);
            return false;
        }


        private void RemovedCallback(CacheEntryRemovedArguments arguments)
        {
            // if we are not removing the item cos it was removed by the caller then save it to disk
            if (arguments.RemovedReason == CacheEntryRemovedReason.Evicted || arguments.RemovedReason == CacheEntryRemovedReason.Expired)
            {
                File.WriteAllText(Path.Combine(BaseDirectory, GetSafeFileName(arguments.CacheItem.Key)), arguments.CacheItem.Value.ToJson());
            }
        }

        private string GetSafeFileName(string filename)
        {
            filename = Hash(filename);

            Array.ForEach(Path.GetInvalidFileNameChars(), c => filename = filename.Replace(c.ToString(), "_"));

            return filename + ".cache";
        }


        private string Hash(string s)
        {
            var buffer = Encoding.UTF8.GetBytes(s);
            var hashedBuffer = _hash.ComputeHash(buffer);

            return Convert.ToBase64String(hashedBuffer);
        }
    }
}
