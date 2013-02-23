using System;
using System.Collections.Generic;
using ServiceStack.Text;

namespace PersistentCache
{
    public class FileIndex : FileBase
    {
        private readonly int _keyLength;
        private readonly int _dataLength;
        private readonly int _intLength;


        public FileIndex(string baseDirectory, int keyLength) : base(baseDirectory)
        {
            _keyLength = keyLength;

            _intLength = int.MaxValue.ToUtf8Bytes().Length;
            _dataLength = _keyLength + (_intLength * 2);
        }


        public void Add(string key, int start, int end)
        {
            var keyBytes = key.ToUtf8Bytes();
            
            var dataBytes = new byte[_intLength * 2];
            
            var temp = start.ToUtf8Bytes();
            temp.CopyTo(dataBytes, 0);

            temp = end.ToUtf8Bytes();
            temp.CopyTo(dataBytes, _intLength);

            base.Write(new List<byte[]>() { keyBytes, dataBytes });
        }

        public bool Contains(string key)
        {
            var position = 0;    
            while (position < base.NextWritePosition)
            {
                var k = base.Read(position, _keyLength);
                if (k == key)
                    return true;

                position += _dataLength;
            }

            return false;
        }

        public bool GetDataPosition(string key, out int start, out int end)
        {
            start = 0;
            end = 0;

            var position = GetKeyPosition(key);
            if (position == -1)
                return false;
            
            position = position + _keyLength;
            start = Convert.ToInt32(Read(position, _intLength));

            position = position + _intLength;
            end = Convert.ToInt32(Read(position, _intLength));

            return true;
        }

        private int GetKeyPosition(string key)
        {
            var position = 0;
            while (position < base.NextWritePosition)
            {
                var k = base.Read(position, _keyLength);
                if (k == key)
                    return position;

                position += _dataLength;
            }

            return -1;
        }
    }
}