using ProtoBuf;

namespace PersistentCache
{
	[ProtoContract]
	public class Thing
	{
		public int Value { get; set; }
	}
}