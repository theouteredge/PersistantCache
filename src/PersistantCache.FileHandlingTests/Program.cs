using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using PersistentCache;
using ServiceStack.Text;

namespace PersistantCache.FileHandlingTests
{
    class Program
    {
        private static KeyFileIndex _keyIndex;

        static void Main(string[] args)
        {
            var key = Hash(Guid.NewGuid().ToString());
            var length = key.ToUtf8Bytes().Length;

            _keyIndex = new KeyFileIndex("C:\\tmp\\PersistentCache", length);

            for (var i = 0; i < 200; i++)
            {
                WriteAKeyToTheIndex(i);
            }

            Console.ReadKey();
        }

        private static void WriteAKeyToTheIndex(int i)
        {
            var result = false;
            int length;
            int start;

            var key = Hash(Guid.NewGuid().ToString());

            _keyIndex.Add(key, 1000 * i, 10000 * i);

            Console.WriteLine(_keyIndex.Contains(key)
                                  ? "We found the {0} key in the file :D"
                                  : "We couldn't find the {0} key in the file O_o", i + 1);


            result = _keyIndex.GetDataPosition(key, out start, out length);
            if (start == 1000 * i && length == 10000 * i)
                Console.WriteLine("Writing {0} Entry Works!", i + 1);
            else
                Console.WriteLine("Writing {0} Entry Failed! O_o", i + 1);
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
