using System;
using System.IO;
using ServiceStack.Text;

namespace PersistentCache.DiskCache
{
    public class FileCache : ICacheToDisk
    {
        private readonly string _baseDirectory;



        public FileCache(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
        }



        public bool Contains(string key)
        {
            var filename = GetSafeFileName(key);
            return File.Exists(Path.Combine(_baseDirectory, filename));
        }

        public void Put(string key, object value)
        {
            File.WriteAllText(Path.Combine(_baseDirectory, GetSafeFileName(key)), value.ToJson());
        }



        public T Get<T>(string key)
        {
            try
            {
                var filename = GetSafeFileName(key);
                return File.ReadAllText(_baseDirectory + filename).FromJson<T>();
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
                    var filename = GetSafeFileName(key);
                    value = File.ReadAllText(_baseDirectory + filename).FromJson<T>();
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
                var filename = GetSafeFileName(key);
                return File.ReadAllText(_baseDirectory + filename);
            }
            catch (Exception)
            {
                return null;
            }
        }



        private static string GetSafeFileName(string filename)
        {
            Array.ForEach(Path.GetInvalidFileNameChars(), c => filename = filename.Replace(c.ToString(), "_"));
            return filename + ".cache";
        }

        public void Dispose()
        {
            foreach(var file in Directory.EnumerateFiles(_baseDirectory, "*.cache"))
                File.Delete(file);
        }
    }
}
