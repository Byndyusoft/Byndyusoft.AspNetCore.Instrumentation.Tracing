using System.Text.Json;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization;
using Moq;
using Xunit;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Unit
{
    public class AspNetMvcRequestTracingOptionsTests
    {
        [Fact]
        public void Configure()
        {
            // arrange
            var serializer = Mock.Of<ISerializer>();
            var limit = 100;

            var mvcOptions = new AspNetMvcTracingOptions
                {Serializer = serializer, ValueMaxStringLength = limit};
            var mvcRequestOptions = new AspNetMvcRequestTracingOptions();

            // act
            mvcRequestOptions.Configure(mvcOptions);

            // assert
            Assert.Same(mvcOptions.Serializer, mvcRequestOptions.Serializer);
            Assert.Equal(mvcOptions.ValueMaxStringLength, mvcRequestOptions.ValueMaxStringLength);
        }
    }
}