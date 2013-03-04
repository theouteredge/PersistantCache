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
                return ReadAllText(Path.Combine(directory, FILENAME)).FromJson<T>();
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
                    value = ReadAllText(Path.Combine(directory, FILENAME)).FromJson<T>();
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
                    return ReadAllText(Path.Combine(directory, FILENAME));
                }
                
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

		private static string ReadAllText(string filePath)
		{
			using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				using (var rdr = new StreamReader(fs))
				{
					return rdr.ReadToEnd();
				}
			}
		}

        private string GetSafeDirectoryName(string key)
        {
            return Path.Combine(_baseDirectory,key);
        }

        public void Dispose()
        {
            Directory.Delete(_baseDirectory, true);
        }
    }
}