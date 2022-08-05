using System.Text.Json;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization.Json;
using Xunit;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Unit
{
    public class SystemTextJsonSerializerTests
    {
        [Fact]
        public async Task SerializeAsync()
        {
            // arrange
            var value = new {Key = "key", Value = "value"};
            var serializer = new SystemTextJsonSerializer();
            var options = new AspNetMvcTracingOptions
            {
                Serializer = serializer
            };

            // act
            var result = await options.SerializeAsync(value);

            // assert
            var expected = JsonSerializer.Serialize(value, serializer.Options);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task SerializeAsync_NullValue()
        {
            // arrange
            var options = new AspNetMvcTracingOptions
            {
                Serializer = new SystemTextJsonSerializer()
            };

            // act
            var result = await options.SerializeAsync(null);

            // assert
            Assert.Equal("null", result);
        }
    }
}