using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Newtonsoft;
using Byndyusoft.MaskedSerialization.Annotations.Attributes;
using Byndyusoft.MaskedSerialization.Annotations.Consts;
using Newtonsoft.Json;
using Xunit;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Unit
{
    public class NewtonsoftJsonSerializerTests
    {
        private readonly NewtonsoftJsonSerializer _serializer = new();

        [Fact]
        public async Task SerializeAsync()
        {
            // arrange
            var value = new { Key = "key", Value = "value" };
            var options = new AspNetMvcTracingOptions
            {
                Serializer = _serializer
            };

            // act
            var result = await _serializer.SerializeAsync(value, options);

            // assert
            var expected = JsonConvert.SerializeObject(value, _serializer.Settings);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task SerializeAsync_NullValue()
        {
            // arrange
            var options = new AspNetMvcTracingOptions
            {
                Serializer = _serializer
            };

            // act
            var result = await options.SerializeAsync(null);

            // assert
            Assert.Equal("null", result);
        }

        [Fact]
        public async Task SerializeAsync_MaskedObject()
        {
            // arrange
            var options = new AspNetMvcTracingOptions
            {
                Serializer = _serializer
            };
            var maskedClass = new MaskedClass
            {
                Id = 10,
                Password = "pwd"
            };

            // act
            var result = await options.SerializeAsync(maskedClass);

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

            [Masked]
            public string Password { get; set; } = default!;
        }
    }
}