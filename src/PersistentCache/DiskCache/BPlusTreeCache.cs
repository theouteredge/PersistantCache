using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CSharpTest.Net.Collections;
using CSharpTest.Net.Serialization;
using PersistentCache.Util;

namespace PersistentCache.DiskCache
{
    public class BPlusTreeCache : ICacheToDisk
    {
	    private readonly int _uniqueStores;
	    private Dictionary<string, BPlusTree<string, int>> _stores;

	    public BPlusTreeCache(string baseDirectory, int uniqueStores = 2, string allowedKeyCharacters = "0123456789ABCDEF")
		{
		    _uniqueStores = uniqueStores;
		    _stores = new Dictionary<string, BPlusTree<string, int>>(StringComparer.CurrentCultureIgnoreCase);

			// first lets generate a list of store identifiers that we can work work by generating the cartesian product of the 
			// first x characters from the list of characters allowed in the key
			// storeNames now contains the values '00', '01', '02' ...
			var storeNames = Enumerable.Repeat(allowedKeyCharacters, uniqueStores).CartesianProduct();


		    var i = 0;
		    foreach (var storeName in storeNames)
		    {
			    var storeNameString = new string(storeName.ToArray());	// storeName is IEnumerable<char>
				Console.WriteLine("i: " + i + " " + storeNameString);
			    i++;
				var cacheOptions = new BPlusTree<string, int>.Options(PrimitiveSerializer.String, PrimitiveSerializer.Int32, StringComparer.Ordinal)
				{
					CreateFile = CreatePolicy.IfNeeded,
					FileName = Path.Combine(baseDirectory, String.Format("Storage-{0}.dat", storeNameString)),
					FileGrowthRate = 1000
				
					
				};
				var cache = new BPlusTree<string, int>(cacheOptions);
				_stores.Add(storeNameString, cache);
		    }
		}
		
        public bool Contains(string key)
        {
			return GetStore(key).ContainsKey(key);
        }

        public void Put(string key, object value)
        {
	        var store = GetStore(key);
			store[key] = (int) value;
        }

        public T Get<T>(string key)
        {
            try
            {
				return (T)(object)GetStore(key)[key];
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
			if (_stores != null)
			{
				foreach (var key in _stores.Keys)
				{
					var store = _stores[key];
					store.Dispose();
					_stores.Remove(key);
				}

				// tidy up the stores
				_stores.Clear();
				_stores = null;	
			}
        }

		/// <summary>
		/// Returns the specific store that holds the item with the key
		/// </summary>
		private BPlusTree<string, int> GetStore(string key)
		{
			return _stores[key.Substring(0, _uniqueStores)];
		}

    }
}