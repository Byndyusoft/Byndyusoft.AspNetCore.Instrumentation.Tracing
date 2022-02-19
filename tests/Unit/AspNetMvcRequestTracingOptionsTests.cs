using System.Text.Json;
using Xunit;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Unit
{
    public class AspNetMvcRequestTracingOptionsTests
    {
        [Fact]
        public void Configure()
        {
            // arrange
            var serializerOptions = new JsonSerializerOptions();
            var limit = 100;

            var mvcOptions = new AspNetMvcTracingOptions
                {JsonSerializerOptions = serializerOptions, ValueMaxStringLength = limit};
            var mvcRequestOptions = new AspNetMvcRequestTracingOptions();

            // act
            mvcRequestOptions.Configure(mvcOptions);

            // assert
            Assert.Same(mvcOptions.JsonSerializerOptions, mvcRequestOptions.JsonSerializerOptions);
            Assert.Equal(mvcOptions.ValueMaxStringLength, mvcRequestOptions.ValueMaxStringLength);
        }
    }
}