using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    internal class StringLimitStream : Stream
    {
        private MemoryStream? _memory;
        private bool _oversized;
        private int? _lengthLimit;

        public StringLimitStream(int? lengthLimit)
        {
            _lengthLimit = lengthLimit;
            _memory = new MemoryStream();
        }

        public override void Flush() => Inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => Inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => Inner.Seek(offset, origin);

        public override void SetLength(long value) => Inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            var inner = Inner;

            if (_lengthLimit != null && inner.Length + count > _lengthLimit)
            {
                _oversized = true;
                count = _lengthLimit.Value - (int)inner.Length;
            }

            Inner.Write(buffer, offset, count);
        }

        public override bool CanRead => Inner.CanRead;
        public override bool CanSeek => Inner.CanSeek;
        public override bool CanWrite => Inner.CanWrite;
        public override long Length => Inner.Length;
        public override long Position
        {
            get => Inner.Position;
            set => Inner.Position = value;
        }

        private MemoryStream Inner => _memory ?? throw new ObjectDisposedException(nameof(StringLimitStream));

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _memory?.Dispose();
                _memory = null;
            }

            base.Dispose(disposing);
        }

        public string GetString()
        {
            var str = Encoding.UTF8.GetString(Inner.ToArray());
            return _oversized ? $"{str}..." : str;
        }
    }
}