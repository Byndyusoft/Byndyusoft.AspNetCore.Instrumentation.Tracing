using System;
using System.Text;
using Xunit;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Unit
{
    public class StringLimitStreamTests
    {
        [Fact]
        public void Write_NoLimit()
        {
            // arrange
            var stream = new StringLimitStream(null);
            var str = "test";

            // act
            stream.Write(Encoding.UTF8.GetBytes(str));

            // assert
            var actual = stream.GetString();
            Assert.Equal(str, actual);
        }

        [Fact]
        public void Write_Limit()
        {
            // arrange
            var stream = new StringLimitStream(5);
            var str = "1234567890";

            // act
            stream.Write(Encoding.UTF8.GetBytes(str));

            // assert
            var actual = stream.GetString();
            Assert.Equal("12345...", actual);
        }

        [Fact]
        public void Dispose_Test()
        {
            // arrange
            var stream = new StringLimitStream(null);

            // act
            stream.Dispose();

            // assert
            var exception = Assert.Throws<ObjectDisposedException>(() => stream.Write(Array.Empty<byte>()));
            Assert.Equal(nameof(StringLimitStream), exception.ObjectName);

        }
    }
}