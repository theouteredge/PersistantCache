using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PersistentCache.DiskCache;
using PersistentCache.InmemoryCache;

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
    /// Once the available memory starts to run out and items are evicted or expired from the in memory cache they will be persisted to the disk based cache.
    /// 
    /// Persistant Cache will first check the 
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


            // 28secs for 2,000,000. ok... 
            _cache = new StdMemoryCache(config, RemovedCallback);
            
            // 15secs for 2,000,000. With basic cache size management, needs to be upgraded to manage actual application memory usage
            //_cache = new BlockingCache(10000, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), RemovedCallback);
            //_diskCache = new DirectoryCache(BaseDirectory);
            //_diskCache = new BPlusTreeCache<T>(BaseDirectory, diskCacheDepth);
        }


        public void Put(string key, object value, int itemExpiration = 10)
        {
            //_cache.Set(Hash(key), value, new CacheItemPolicy() { RemovedCallback = RemovedCallback });
            _cache.TryAdd(Hash(key), value);
        }


        public bool TryGet<T>(string key, out T value)
        {
            key = Hash(key);

            //var valueTmp = _cache.Get(key);
            //if (valueTmp != null)

            object valueTmp;
            if (_cache.TryGet(key, out valueTmp))
            {
                value = (T) valueTmp;
                return true;
            }

            if (_itemsCachedToDisk)
            {
                // it wasn't in the memory cache, so look for it within the ICacheToDisk
	            T item;
				if (_diskCache.TryGet(key, out item))
	            {
		            value = item;
		            return true;
	            }
            }

            value = default(T);
            return false;
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
	        var buffer = Encoding.UTF8.GetBytes(s);
            var hashedBuffer = hash.ComputeHash(buffer);
			var result = ToHex(hashedBuffer);

            return result;
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

		/// <summary>
		/// Returns a hex string representation of an array of bytes.
		/// </summary>
		/// <param name="value">The array of bytes.</param>
		/// <returns>A hex string representation of the array of bytes.</returns>
		private static string ToHex(IEnumerable<byte> value)
		{
			var sb = new StringBuilder();
			if (value != null)
				foreach (var b in value)
					sb.Append(HexStringTable[b]);

			return sb.ToString();
		}


        /// <summary>
        /// Hex string lookup table. Faster to have them in a string array
        /// </summary>
        private static readonly string[] HexStringTable = new[]
				{
					"00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "0A", "0B", "0C", "0D", "0E", "0F",
					"10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "1A", "1B", "1C", "1D", "1E", "1F",
					"20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "2A", "2B", "2C", "2D", "2E", "2F",
					"30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "3A", "3B", "3C", "3D", "3E", "3F",
					"40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "4A", "4B", "4C", "4D", "4E", "4F",
					"50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "5A", "5B", "5C", "5D", "5E", "5F",
					"60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "6A", "6B", "6C", "6D", "6E", "6F",
					"70", "71", "72", "73", "74", "75", "76", "77", "78", "79", "7A", "7B", "7C", "7D", "7E", "7F",
					"80", "81", "82", "83", "84", "85", "86", "87", "88", "89", "8A", "8B", "8C", "8D", "8E", "8F",
					"90", "91", "92", "93", "94", "95", "96", "97", "98", "99", "9A", "9B", "9C", "9D", "9E", "9F",
					"A0", "A1", "A2", "A3", "A4", "A5", "A6", "A7", "A8", "A9", "AA", "AB", "AC", "AD", "AE", "AF",
					"B0", "B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8", "B9", "BA", "BB", "BC", "BD", "BE", "BF",
					"C0", "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "CA", "CB", "CC", "CD", "CE", "CF",
					"D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "DA", "DB", "DC", "DD", "DE", "DF",
					"E0", "E1", "E2", "E3", "E4", "E5", "E6", "E7", "E8", "E9", "EA", "EB", "EC", "ED", "EE", "EF",
					"F0", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "FA", "FB", "FC", "FD", "FE", "FF"
				};
    }
}