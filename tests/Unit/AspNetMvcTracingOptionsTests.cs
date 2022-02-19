using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Unit
{
    public class AspNetMvcTracingOptionsTests
    {
        [Fact]
        public void Constructor_DefaultValueMaxStringLength()
        {
            // act
            var options = new AspNetMvcTracingOptions();

            // assert
            Assert.Equal(AspNetMvcTracingOptions.DefaultValueMaxStringLength, options.ValueMaxStringLength);
        }

        [Fact]
        public void Constructor_Adds_JsonStringEnumConverter()
        {
            // act
            var options = new AspNetMvcTracingOptions();

            // assert
            Assert.Contains(options.JsonSerializerOptions.Converters, c => c is JsonStringEnumConverter);
        }

        [Fact]
        public void JsonSerializerOptions_Setter()
        {
            // arrange
            var options = new AspNetMvcTracingOptions();
            var jsonOptions = new JsonSerializerOptions();

            // act
            options.JsonSerializerOptions = jsonOptions;

            // assert
            Assert.Same(jsonOptions, options.JsonSerializerOptions);
        }

        [Fact]
        public void JsonSerializerOptions_Setter_Null_ThrowsException()
        {
            // arrange
            var options = new AspNetMvcTracingOptions();

            // act
            var exception = Assert.Throws<ArgumentNullException>(() => options.JsonSerializerOptions = null!);

            // assert
            Assert.Equal(nameof(AspNetMvcTracingOptions.JsonSerializerOptions), exception.ParamName);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(null)]
        public void ValueMaxStringLength_Setter(int? value)
        {
            // arrange
            var options = new AspNetMvcTracingOptions();

            // act
            options.ValueMaxStringLength = value;

            // assert
            Assert.Equal(value, options.ValueMaxStringLength);
        }

        [Fact]
        public void ValueMaxStringLength_Setter_NegativeValue_ThrowsException()
        {
            // arrange
            var options = new AspNetMvcTracingOptions();

            // act
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => options.ValueMaxStringLength = -1);

            // assert
            Assert.Equal(nameof(AspNetMvcTracingOptions.ValueMaxStringLength), exception.ParamName);
        }
    }
}
