using System;
using System.IO;
using System.Linq;
using ServiceStack.Text;

namespace PersistentCache.DiskCache
{
    public class DirectoryCache : ICacheToDisk
    {
        private const string FILENAME = "value.cache";
        private readonly string _baseDirectory;



        public DirectoryCache(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
        }



        public bool Contains(string key)
        {
            return Directory.Exists(GetSafeDirectoryName(key));
        }

        public void Put(string key, object value)
        {
            var directory = GetSafeDirectoryName(key);
            
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, FILENAME), value.ToJson());
        }



        public T Get<T>(string key)
        {
            try
            {
                var directory = GetSafeDirectoryName(key);
                return File.ReadAllText(Path.Combine(directory, FILENAME)).FromJson<T>();
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        public bool TryGet<T>(string key, out T value)
        {
            try
            {
                if (Contains(key))
                {
                    var directory = GetSafeDirectoryName(key);
                    value = File.ReadAllText(Path.Combine(directory, FILENAME)).FromJson<T>();
                    return true;
                }

                value = default(T);
                return false;
            }
            catch (Exception)
            {
                value = default(T);
                return false;
            }
        }

        public string Get(string key)
        {
            try
            {
                if (Contains(key))
                {
                    var directory = GetSafeDirectoryName(key);
                    return File.ReadAllText(Path.Combine(directory, FILENAME));
                }
                
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }



        private string GetSafeDirectoryName(string key)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var result = new char[key.Length];

            for (var i = 0; i < key.Length; i++)
            {
                if (invalidChars.Contains(key[i]))
                    result[i] = '_';
                else
                    result[i] = key[i];
            }
            //Array.ForEach(Path.GetInvalidFileNameChars(), c => key = key.Replace(c.ToString(), "_"));
            
            return Path.Combine(_baseDirectory, new string(result));
        }

        public void Dispose()
        {
            Directory.Delete(_baseDirectory, true);
        }
    }
}