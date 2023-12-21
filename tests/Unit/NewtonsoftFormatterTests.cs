using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization.Json;
using Byndyusoft.MaskedSerialization.Annotations.Attributes;
using Byndyusoft.MaskedSerialization.Annotations.Consts;
using Newtonsoft.Json;
using Xunit;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Unit
{
    public class NewtonsoftFormatterTests
    {
        private readonly NewtonsoftJsonFormatter _formatter = new();

        [Fact]
        public async Task FormatAsync()
        {
            // arrange
            var value = new { Key = "key", Value = "value" };
            var options = new AspNetMvcTracingOptions
            {
                Formatter = _formatter
            };

            // act
            var result = await _formatter.FormatAsync(value, options);

            // assert
            var expected = JsonConvert.SerializeObject(value, _formatter.Settings);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task SerializeAsync_NullValue()
        {
            // arrange
            var options = new AspNetMvcTracingOptions
            {
                Formatter = _formatter
            };

            // act
            var result = await options.FormatAsync(null);

            // assert
            Assert.Equal("null", result);
        }

        [Fact]
        public async Task SerializeAsync_MaskedObject()
        {
            // arrange
            var options = new AspNetMvcTracingOptions
            {
                Formatter = _formatter
            };
            var maskedClass = new MaskedClass
            {
                Id = 10,
                Password = "pwd"
            };

            // act
            var result = await options.FormatAsync(maskedClass);

            // assert
            Assert.NotNull(result);

            var deserialized = JsonConvert.DeserializeObject<MaskedClass>(result);
            Assert.NotNull(deserialized);
            Assert.Equal(maskedClass.Id, deserialized.Id);
            Assert.Equal(MaskStrings.Default, deserialized.Password);
        }

        private class MaskedClass
        {
            public int Id { get; set; }

            [Masked] public string Password { get; set; } = default!;
        }
    }
}