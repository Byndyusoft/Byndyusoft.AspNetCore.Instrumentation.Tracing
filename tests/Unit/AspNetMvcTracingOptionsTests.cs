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
        public void Formatter_Setter()
        {
            // arrange
            var options = new AspNetMvcTracingOptions();
            var formatter = Mock.Of<IFormatter>();

            // act
            options.Formatter = formatter;

            // assert
            Assert.Same(formatter, options.Formatter);
        }

        [Fact]
        public void Formatter_Setter_Null_ThrowsException()
        {
            // arrange
            var options = new AspNetMvcTracingOptions();

            // act
            var exception = Assert.Throws<ArgumentNullException>(() => options.Formatter = null!);

            // assert
            Assert.Equal(nameof(AspNetMvcTracingOptions.Formatter), exception.ParamName);
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

        [Fact]
        public void Configure()
        {
            // arrange
            var formatter = Mock.Of<IFormatter>();
            var limit = 100;

            var mvcOptions = new AspNetMvcTracingOptions
                { Formatter = formatter, ValueMaxStringLength = limit };
            var mvcRequestOptions = new AspNetMvcTracingOptions();

            // act
            mvcRequestOptions.Configure(mvcOptions);

            // assert
            Assert.Same(mvcOptions.Formatter, mvcRequestOptions.Formatter);
            Assert.Equal(mvcOptions.ValueMaxStringLength, mvcRequestOptions.ValueMaxStringLength);
        }
    }
}