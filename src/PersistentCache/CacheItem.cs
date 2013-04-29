using System;
using ProtoBuf;

namespace PersistentCache
{
	[ProtoContract]
    [Serializable]
	public class CacheItem
	{
		public int Value { get; set; }
	}
}