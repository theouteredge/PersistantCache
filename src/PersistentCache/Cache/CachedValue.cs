using System;
using System.Threading;

namespace PersistentCache.Cache
{
    public class CachedValue
    {
        private object _value;
        private int _hitCount;


        public DateTime LastHitAt { get; set; }
        public int HitCount
        {
            get { return _hitCount; }
            set { _hitCount = value; }
        }

        public object Value
        {
            get
            {
                Interlocked.Increment(ref _hitCount);
                LastHitAt = DateTime.Now;

                return _value;
            }
            set
            {
                Interlocked.Increment(ref _hitCount);
                LastHitAt = DateTime.Now;

                _value = value;
            }
        }

        

        public CachedValue(object value)
        {
            _value = value;
            LastHitAt = DateTime.Now;
            HitCount = 1;
        }
    }
}