using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Unit
{
    public class TracingMvcBuilderExtensionsTests
    {
        private readonly IMvcBuilder _mvcBuilder;
        private readonly IServiceCollection _services;

        public TracingMvcBuilderExtensionsTests()
        {
            _services = new ServiceCollection().AddLogging();
            _mvcBuilder = _services.AddMvc();
        }

        [Fact]
        public void AddTracing()
        {
            // act
            _mvcBuilder.AddTracing();

            // assert
            var provider = _services.BuildServiceProvider();

            var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;

            var responseTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcTracingOptions>>();
            Assert.NotNull(responseTracingOptions);

            var requestTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcTracingOptions>>();
            Assert.NotNull(requestTracingOptions);

            Assert.Contains(mvcOptions.Filters,
                filter => filter is TypeFilterAttribute type &&
                          type.ImplementationType == typeof(AspNetMvcResponseTracingFilter));
            Assert.Contains(mvcOptions.Filters,
                filter => filter is TypeFilterAttribute type &&
                          type.ImplementationType == typeof(AspNetMvcRequestTracingFilter));
        }

        [Fact]
        public void AddTracing_Options()
        {
            // arrange
            var formatter = Mock.Of<IFormatter>();
            var valueMaxStringLength = 1000;

            // act
            _mvcBuilder.AddTracing(
                tracing =>
                {
                    tracing.Formatter = formatter;
                    tracing.ValueMaxStringLength = valueMaxStringLength;
                });

            // assert
            var provider = _services.BuildServiceProvider();

            var requestTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcTracingOptions>>().Value;
            Assert.Same(formatter, requestTracingOptions.Formatter);
            Assert.Equal(valueMaxStringLength, requestTracingOptions.ValueMaxStringLength);

            var responseTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcTracingOptions>>().Value;
            Assert.Same(formatter, responseTracingOptions.Formatter);
            Assert.Equal(valueMaxStringLength, responseTracingOptions.ValueMaxStringLength);
        }
    }
}