using System.Text.Json;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization.Json;
using Xunit;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Unit
{
    public class SystemTextJsonSerializerTests
    {
        private readonly SystemTextJsonSerializer _serializer = new();

        [Fact]
        public async Task SerializeRequestParamAsync()
        {
            // arrange
            var value = new {Key = "key", Value = "value"};
            var options = new AspNetMvcTracingOptions();

            // act
            var result = await _serializer.SerializeAsync(value, options);

            // assert
            var expected = JsonSerializer.Serialize(value, _serializer.Options);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task SerializeRequestParamAsync_NullValue()
        {
            // arrange
            var options = new AspNetMvcTracingOptions();

            // act
            var result = await _serializer.SerializeAsync(null, options);

            // assert
            Assert.Equal("null", result);
        }

        [Fact]
        public async Task SerializeResponseBodyAsync()
        {
            // arrange
            var value = new {Key = "key", Value = "value"};
            var options = new AspNetMvcTracingOptions();

            // act
            var result = await _serializer.SerializeAsync(value, options);

            // assert
            var expected = JsonSerializer.Serialize(value, _serializer.Options);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task SerializeResponseBodyAsync_NullValue()
        {
            // arrange
            var options = new AspNetMvcTracingOptions();

            // act
            var result = await _serializer.SerializeAsync(null, options);

            // assert
            Assert.Null(result);
        }
    }
}