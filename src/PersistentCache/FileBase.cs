using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PersistentCache
{
    public class FileBase
    {
        private readonly object _lock = new object();

        protected int Filesize = 0;
        protected int NextWritePosition = 0;

        private readonly FileStream _writeFs;
        private readonly FileStream _readFs;


        protected FileBase(string baseDirectory, int filesize = 1048576)
        {
            Filesize = filesize;

            _writeFs = new FileStream(Path.Combine(baseDirectory, "index.cache"), FileMode.Create, FileAccess.Write, FileShare.Read);
            _readFs = new FileStream(Path.Combine(baseDirectory, "index.cache"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            ResizeFile(Filesize);
        }


        protected void Write(List<byte[]> data)
        {
            var length = data.Sum(x => x.Length);

            lock (_lock)
            {
                // will the next write go over then end of the file? If so, make the file bigger
                if (NextWritePosition + length > Filesize)
                    ResizeFile(Filesize);

                _writeFs.Seek(NextWritePosition, SeekOrigin.Begin);

                foreach(var d in data)
                    _writeFs.Write(d, 0, d.Length);

                _writeFs.Flush();

                NextWritePosition += length;
            }
        }

        protected string Read(int start, int length)
        {
            var result = new byte[length];

            _readFs.Seek(start, SeekOrigin.Begin);
            _readFs.Read(result, 0, length);

            return Encoding.UTF8.GetString(result, 0, length);
        }

        protected void ResizeFile(int size)
        {
            lock (_lock)
            {
                _writeFs.SetLength(_writeFs.Length + size);
                _writeFs.Flush();

                Filesize += size;
            }
        }
    }
}