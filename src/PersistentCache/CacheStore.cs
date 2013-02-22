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
    public class CacheStore
    {
        private readonly MemoryCache _cache;

        public string PollingInterval { get; set; }
        public string PhysicalMemoryLimitPercentage { get; set; }
        public string CacheMemoryLimitMegabytes { get; set; }

        public string BaseDirectory { get; set; }



        public CacheStore()
        {
            var config = new NameValueCollection
                {
                    {"pollingInterval", PollingInterval ?? "00:01:00" },
                    {"physicalMemoryLimitPercentage", PhysicalMemoryLimitPercentage ?? "0" },
                    {"cacheMemoryLimitMegabytes", CacheMemoryLimitMegabytes ?? "100" }
                };

            _cache = new MemoryCache("MainCache", config);
        }


        public void Put(string key, object value, int itemExpiration = 10)
        {
            _cache.Set(Hash(key), value, new CacheItemPolicy() { RemovedCallback = RemovedCallback });
        }


        public bool TryGet<T>(string key, out T value)
        {
            key = Hash(key);

            var valueTmp = _cache.Get(key);
            if (valueTmp != null)
            {
                value = (T)valueTmp;
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

        private static string GetSafeFileName(string filename)
        {
            Array.ForEach(Path.GetInvalidFileNameChars(), c => filename = filename.Replace(c.ToString(), "_"));
            return filename + ".cache";
        }

        private static string Hash(string s)
        {
            var hash = new SHA1Managed();
            var result = "";

            var buffer = Encoding.UTF8.GetBytes(s);
            var hashedBuffer = hash.ComputeHash(buffer);

            result = Convert.ToBase64String(hashedBuffer);
            
            return result;
        }
    }
}