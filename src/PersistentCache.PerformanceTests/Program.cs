using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace PersistentCache.PerformanceTests
{
    class Program
    {
        private static readonly CacheStore PersistentCache = new CacheStore("c:\\tmp\\PersistentCache", "1", null, "00:00:10");


        static void Main(string[] args)
        {
            Console.WriteLine("Creating Data");
            const int itemsToUse = 1000000;

            var items = GenerateList(itemsToUse, 20);
            items.Shuffle();

            //var temp = new CacheItem[itemsToUse];
            //items.CopyTo(temp);
            //var reversedItems = temp.Reverse().ToList();

            RunPersistanceCacheTest(new List<List<CacheItem>>(2) { items });
            //RunDiskCacheTest(new List<List<CacheItem>>(2) { items });


            Console.ReadKey();
        }

        private static List<CacheItem> GenerateList(int itemsToUse, double percentageOfDuplicates)
        {
            var percentageOfUniques = (100.0 - percentageOfDuplicates) / 100.0;
            percentageOfDuplicates = 1 - (percentageOfUniques);

            var uniqueCount = (int) Math.Ceiling(itemsToUse*percentageOfUniques);
            var duplicateCount = (int) Math.Ceiling(itemsToUse*percentageOfDuplicates);

            var results = new List<CacheItem>(uniqueCount);
            var duplicates = new List<CacheItem>(duplicateCount);

            for (var i = 0; i < uniqueCount; i++)
            {
                results.Add(new CacheItem() { Key = Guid.NewGuid().ToString(), Value = i });
            }

            var random = new Random(DateTime.Now.Millisecond);
            for (var i = 0; i < duplicateCount; i++)
            {
                var index = random.Next(uniqueCount);
                duplicates.Add(results[index]);
            }
            
            results.AddRange(duplicates);
            return results;
        }



        private static void RunPersistanceCacheTest(List<List<CacheItem>> data)
        {
            Console.WriteLine("Starting {0} threads", data.Count);
            var thread = 0;
            var threads = new List<Task>(data.Count);

            foreach (var set in data)
            {
                threads.Add(Task.Factory.StartNew(() => RunThreadPersistancCacheTest(set, thread++)));
            }

            Console.WriteLine("Executing");
            Console.WriteLine("");

            Task.WaitAll(threads.ToArray());
            
            Console.WriteLine("cleaning up...");
            
            var sw = new Stopwatch();
            
            sw.Start();
            PersistentCache.Dispose();
            sw.Stop();

            Console.WriteLine("clean in {0}ms :D", sw.ElapsedMilliseconds);
        }


        private static void RunThreadPersistancCacheTest(IEnumerable<CacheItem> items, int threadNo)
        {
            var stopwatch = new Stopwatch();
            var cacheHits = 0;
            var cacheMiss = 0;
            var exceptions = 0;
            var storageExceptions = 0;
            var count = 0;
            long lastInterval = 0;

            stopwatch.Start();
            foreach (var item in items)
            {
                count++;

                var value = 0;
                if (PersistentCache.TryGet(item.Key, out value))
                {
                    cacheHits++;
                }
                else
                {
                    cacheMiss++;
                    PersistentCache.Put(item.Key, item.Value);

                    if (!PersistentCache.TryGet(item.Key, out value))
                        exceptions++;
                }

                if (value != item.Value)
                {
                    storageExceptions++;
                }

                if (count % 1000 == 0)
                {
                    Console.WriteLine("Thread: {0} :: {1} processed ... {2}ms", threadNo, count, lastInterval == 0 ? stopwatch.ElapsedMilliseconds : stopwatch.ElapsedMilliseconds - lastInterval);
                    lastInterval = stopwatch.ElapsedMilliseconds;
                }
            }
            stopwatch.Stop();

            Console.WriteLine("Threa: {5} :: Test run in {0}ms, with {1} hits and {2} misses, exceptions {3} and storage exceptions {4}", stopwatch.ElapsedMilliseconds, cacheHits, cacheMiss, exceptions, storageExceptions, threadNo);
        }
    }
}
