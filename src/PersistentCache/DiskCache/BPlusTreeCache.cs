using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CSharpTest.Net.Collections;
using CSharpTest.Net.Serialization;
using CSharpTest.Net.Synchronization;
using PersistentCache.Util;

namespace PersistentCache.DiskCache
{
    public class BPlusTreeCache<T> : ICacheToDisk
    {
        private readonly int _uniqueStores;
        private Dictionary<string, BPlusTree<string, T>> _stores;
        private bool _disposing = false;

        public class ProtoNetSerializer<T> : ISerializer<T>
        {
            public T ReadFrom(Stream stream)
            {
                return ProtoBuf.Serializer.DeserializeWithLengthPrefix<T>(stream, ProtoBuf.PrefixStyle.Base128);
            }
            public void WriteTo(T value, Stream stream)
            {
                ProtoBuf.Serializer.SerializeWithLengthPrefix<T>(stream, value, ProtoBuf.PrefixStyle.Base128);
            }
        }

        public BPlusTreeCache(string baseDirectory, int uniqueStores = 2, string allowedKeyCharacters = "0123456789ABCDEF")
        {
            _uniqueStores = uniqueStores;
            _stores = new Dictionary<string, BPlusTree<string, T>>(StringComparer.CurrentCultureIgnoreCase);

            var storeNames = GenerateUniqueStoreNames(uniqueStores, allowedKeyCharacters);
            foreach (var storeName in storeNames)
            {
                var storeNameString = new string(storeName.ToArray());	// storeName is IEnumerable<char>
                var cacheOptions = new BPlusTree<string, T>.Options(PrimitiveSerializer.String, new ProtoNetSerializer<T>(), StringComparer.Ordinal)
                {
                    CreateFile = CreatePolicy.IfNeeded,
                    FileName = Path.Combine(baseDirectory, String.Format("Storage-{0}.dat", storeNameString)),
                    FileGrowthRate = 1000, 
                    CallLevelLock = new ReaderWriterLocking()
                };
                var cache = new BPlusTree<string, T>(cacheOptions);
                _stores.Add(storeNameString, cache);
            }
        }

        /// <summary>
        /// generate a list of store identifiers that we can work work by generating the cartesian product of the 
        /// first x characters from the list of characters allowed in the key
        /// storeNames now contains the values '00', '01', '02' ...
        /// </summary>
        private IEnumerable<IEnumerable<char>> GenerateUniqueStoreNames(int uniqueStores, string allowedKeyCharacters)
        {
            IList<IEnumerable<char>> storeNames;

            if (uniqueStores > 1)
                storeNames = Enumerable.Repeat(allowedKeyCharacters, uniqueStores).CartesianProduct();
            else
            {
                storeNames = new List<IEnumerable<char>>();
                foreach (var ch in allowedKeyCharacters)
                    storeNames.Add(new string(new char[] { ch }));
            }

            return storeNames;
        }

        public bool Contains(string key)
        {
            var store = GetStore(key);
            if (store == null)
                return false;

            return store.ContainsKey(key);
        }

        private object _lock = new object();
        public void Put(string key, object value)
        {
            lock (_lock)
            {
                var store = GetStore(key);
                if (store != null)
                    store[key] = (T)value;
            }
        }

        public TValue Get<TValue>(string key)
        {
            try
            {
                TValue result;
                lock (_lock)
                {
                    var store = GetStore(key);
                    if (store != null)
                        result = (TValue)(store[key] as object);
                    else
                        result = default(TValue);
                }

                return result;
            }
            catch (Exception)
            {
                return default(TValue);
            }
        }

        public bool TryGet<TValue>(string key, out TValue value)
        {
            try
            {
                lock (_lock)
                {
                    if (Contains(key))
                    {
                        value = Get<TValue>(key);
                        return true;
                    }

                    value = default(TValue);
                    return false;
                }
            }
            catch (Exception)
            {
                value = default(TValue);
                return false;
            }
        }

        public void Dispose()
        {
            _disposing = true;

            if (_stores != null)
            {
                foreach (var key in _stores.Keys)
                {
                    var store = _stores[key];
                    if (store != null)
                    {
                        try
                        {
                            store.Dispose();
                        }
                        catch (Exception)
                        {
                            //dirty, dirty, dirty
                        }

                        store = null;
                    }
                }

                // tidy up the stores
                _stores.Clear();
                _stores = null;
            }
        }

        /// <summary>
        /// Returns the specific store that holds the item with the key
        /// </summary>
        private BPlusTree<string, T> GetStore(string key)
        {
            return _disposing ? null : _stores[key.Substring(0, _uniqueStores)];
        }
    }
}