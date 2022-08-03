using System;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization;
using Moq;
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
        public void Serializer_Setter()
        {
            // arrange
            var options = new AspNetMvcTracingOptions();
            var serializer = Mock.Of<ISerializer>();

            // act
            options.Serializer = serializer;

            // assert
            Assert.Same(serializer, options.Serializer);
        }

        [Fact]
        public void Serializer_Setter_Null_ThrowsException()
        {
            // arrange
            var options = new AspNetMvcTracingOptions();

            // act
            var exception = Assert.Throws<ArgumentNullException>(() => options.Serializer = null!);

            // assert
            Assert.Equal(nameof(AspNetMvcTracingOptions.Serializer), exception.ParamName);
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