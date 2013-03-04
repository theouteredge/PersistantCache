using System;
using DBreeze;

namespace PersistentCache.DiskCache
{
    public class DBreezeCache : ICacheToDisk
    {
	    private readonly DBreezeEngine _engine;
	    private readonly string _tableName;

	    public DBreezeCache(string baseDirectory)
		{
			_engine = new DBreezeEngine(baseDirectory);
		    _tableName = Guid.NewGuid().ToString();
		}

        public bool Contains(string key)
        {
            using (var tran = _engine.GetTransaction())
            {
	            var row = tran.Select<string, object>(_tableName, key);
	            return (row != null && row.Exists);
            }
        }

        public void Put(string key, object value)
        {
			using (var tran = _engine.GetTransaction())
			{
				tran.Insert<string, int>(_tableName, key, (int) value);
				tran.Commit();
			}
        }

        public T Get<T>(string key)
        {
            try
            {
				using (var tran = _engine.GetTransaction())
				{
					var row = tran.Select<string, object>(_tableName, key);
					return (T) (row.Value) ;
				}
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
	               value = Get<T>(key);
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
	                return Get<string>(key);
                
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Dispose()
        {
			if (_engine != null)
			{
				using (var tran = _engine.GetTransaction())
				{
					tran.RemoveAllKeys(_tableName, false);
				}

				_engine.Dispose();
			}
			

			
        }
    }
}