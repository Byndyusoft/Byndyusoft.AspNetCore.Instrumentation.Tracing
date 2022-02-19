using System.Text.Json;
using Xunit;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Unit
{
    public class AspNetMvcResponseTracingOptionsTests
    {
        [Fact]
        public void Configure()
        {
            // arrange
            var serializerOptions = new JsonSerializerOptions();
            var limit = 100;

            var mvcOptions = new AspNetMvcTracingOptions
                { JsonSerializerOptions = serializerOptions, ValueMaxStringLength = limit };
            var mvcResponseOptions = new AspNetMvcResponseTracingOptions();

            // act
            mvcResponseOptions.Configure(mvcOptions);

            // assert
            Assert.Same(mvcOptions.JsonSerializerOptions, mvcResponseOptions.JsonSerializerOptions);
            Assert.Equal(mvcOptions.ValueMaxStringLength, mvcResponseOptions.ValueMaxStringLength);
        }
    }
}