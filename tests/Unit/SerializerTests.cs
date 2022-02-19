using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Unit
{
    public class SerializerTests
    {
        private readonly Serializer _serializer = new Serializer();

        [Fact]
        public async Task SerializeRequestParamAsync()
        {
            // arrange
            var value = new {Key = "key", Value = "value"};
            var options = new AspNetMvcRequestTracingOptions();

            // act
            var result = await _serializer.SerializeRequestParamAsync(value, options);

            // assert
            var expected = JsonSerializer.Serialize(value, options.JsonSerializerOptions);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task SerializeRequestParamAsync_NullValue()
        {
            // arrange
            var options = new AspNetMvcRequestTracingOptions();

            // act
            var result = await _serializer.SerializeRequestParamAsync(null, options);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SerializeResponseBodyAsync()
        {
            // arrange
            var value = new { Key = "key", Value = "value" };
            var options = new AspNetMvcResponseTracingOptions();

            // act
            var result = await _serializer.SerializeResponseBodyAsync(value, options);

            // assert
            var expected = JsonSerializer.Serialize(value, options.JsonSerializerOptions);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task SerializeResponseBodyAsync_NullValue()
        {
            // arrange
            var options = new AspNetMvcResponseTracingOptions();

            // act
            var result = await _serializer.SerializeResponseBodyAsync(null, options);

            // assert
            Assert.Null(result);
        }
    }
}