using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PersistentCache.PerformanceTests
{
    public static class ListExtensions
    {
        static readonly Random Random = new Random(DateTime.Now.Millisecond);

        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;

                var k = Random.Next(n + 1);
                var value = list[k];

                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
