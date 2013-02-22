using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistentCache.PerformanceTests
{
    class Program
    {
        private static readonly PersistentCache _cache = new PersistentCache()
            {
                BaseDirectory = "C:\\tmp\\PersistentCache",
                CacheMemoryLimitMegabytes = "1",
                PollingInterval = "00:00:10"
            };

        static void Main(string[] args)
        {
            Console.WriteLine("Creating Data");
            var itemsToUse = 1000000;

            var items = new List<CacheItem>(itemsToUse);
            var temp = new CacheItem[itemsToUse];

            for (var i = 0; i < itemsToUse; i++)
            {
                items.Add(new CacheItem() { Key = Guid.NewGuid().ToString(), Value = i });
            }

            items.CopyTo(temp);
            var reversedItems = temp.Reverse();



            Console.WriteLine("Starting threads");

            Task.Factory.StartNew(() => RunTest(items));
            Task.Factory.StartNew(() => RunTest(reversedItems));

            Console.WriteLine("Executing");
            Console.WriteLine("");

            Console.ReadKey();
        }

        private static void RunTest(IEnumerable<CacheItem> items)
        {
            var stopwatch = new Stopwatch();
            var cacheHits = 0;
            var cacheMiss = 0;

            stopwatch.Start();
            foreach (var item in items)
            {
                int value = 0;
                if (_cache.TryGet(item.Key, out value))
                {
                    cacheHits++;
                }
                else
                {
                    cacheMiss++;
                    _cache.Put(item.Key, item.Value);
                }
            }
            stopwatch.Stop();

            Console.WriteLine("\tTest run in {0}ms, with {1} hits and {2} misses", stopwatch.ElapsedMilliseconds, cacheHits, cacheMiss);
        }
    }
}
