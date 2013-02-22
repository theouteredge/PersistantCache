using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PersistentCache.PerformanceTests
{
    class Program
    {
        private static readonly Store _cache = new Store()
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
            //Thread.Sleep(5000);
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
            var exceptions = 0;
            var storageExceptions = 0;
            var saveExceptions = 0;

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
                    //if (!_cache.Put(item.Key, item.Value))
                    //{
                    //    // try again
                    //    if (!_cache.Put(item.Key, item.Value))
                    //        saveExceptions++;
                    //}

                    if (!_cache.TryGet(item.Key, out value))
                        exceptions++;
                }

                if (value != item.Value)
                {
                    storageExceptions++;
                }
            }
            stopwatch.Stop();

            Console.WriteLine("\tTest run in {0}ms, with {1} hits and {2} misses, exceptions {3} and storage exceptions {4} and save excpetions {5}", stopwatch.ElapsedMilliseconds, cacheHits, cacheMiss, exceptions, storageExceptions, saveExceptions);
        }
    }
}
