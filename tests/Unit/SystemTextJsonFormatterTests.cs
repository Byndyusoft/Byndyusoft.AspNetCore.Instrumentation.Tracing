using System.Text.Json;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization.Json;
using Xunit;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Unit
{
    public class SystemTextJsonFormatterTests
    {
        [Fact]
        public async Task FormatAsync()
        {
            // arrange
            var value = new {Key = "key", Value = "value"};
            var formatter = new SystemTextJsonFormatter();
            var options = new AspNetMvcTracingOptions
            {
                Formatter = formatter
            };

            // act
            var result = await options.FormatAsync(value);

            // assert
            var expected = JsonSerializer.Serialize(value, formatter.Options);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task FormatAsync_NullValue()
        {
            // arrange
            var options = new AspNetMvcTracingOptions
            {
                Formatter = new SystemTextJsonFormatter()
            };

            // act
            var result = await options.FormatAsync(null);

            // assert
            Assert.Equal("null", result);
        }
    }
}