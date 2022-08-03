using System.Text.Json;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization;
using Moq;
using Xunit;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Unit
{
    public class AspNetMvcResponseTracingOptionsTests
    {
        [Fact]
        public void Configure()
        {
            // arrange
            var serializer = Mock.Of<ISerializer>();
            var limit = 100;

            var mvcOptions = new AspNetMvcTracingOptions
                {Serializer = serializer, ValueMaxStringLength = limit};
            var mvcResponseOptions = new AspNetMvcResponseTracingOptions();

            // act
            mvcResponseOptions.Configure(mvcOptions);

            // assert
            Assert.Same(mvcOptions.Serializer, mvcResponseOptions.Serializer);
            Assert.Equal(mvcOptions.ValueMaxStringLength, mvcResponseOptions.ValueMaxStringLength);
        }
    }
}