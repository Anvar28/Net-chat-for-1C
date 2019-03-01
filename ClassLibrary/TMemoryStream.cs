using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ClassLibrary
{
    // 
    public class TMemoryStream
    {
        private MemoryStream _ms;

        public MemoryStream ms { get { return _ms; } }
        public long Length { get { return _ms.Length; } }
        public long Position { get { return _ms.Position; } set { _ms.Position = value; } }

        public TMemoryStream()
        {
            _ms = new MemoryStream();
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            _ms.Write(buffer, offset, count);
        }

        public byte[] ToArray()
        {
            return _ms.ToArray();
        }

        public void TruncateFromTop(int numberOfBytesToRemove)
        {
            byte[] buf = _ms.GetBuffer();
            Buffer.BlockCopy(buf, numberOfBytesToRemove, buf, 0, (int)_ms.Length - numberOfBytesToRemove);
            _ms.SetLength(_ms.Length - numberOfBytesToRemove);
        }

        public void Clear()
        {
            Array.Clear(_ms.GetBuffer(), 0, _ms.GetBuffer().Length);
            _ms.Position = 0;
        }

    }
}
