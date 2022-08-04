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
        public void AddRequestTracing()
        {
            // act
            _mvcBuilder.AddRequestTracing();

            // assert
            var provider = _services.BuildServiceProvider();

            var requestTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcTracingOptions>>();
            Assert.NotNull(requestTracingOptions);

            var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
            Assert.Contains(mvcOptions.Filters,
                filter => filter is TypeFilterAttribute type &&
                          type.ImplementationType == typeof(AspNetMvcRequestTracingFilter));
        }

        [Fact]
        public void AddRequestTracing_Options()
        {
            // arrange
            var serializer = Mock.Of<ISerializer>();
            var valueMaxStringLength = 1000;

            // act
            _mvcBuilder.AddRequestTracing(
                tracing =>
                {
                    tracing.Serializer = serializer;
                    tracing.ValueMaxStringLength = valueMaxStringLength;
                });

            // assert
            var provider = _services.BuildServiceProvider();

            var requestTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcTracingOptions>>().Value;
            Assert.Same(serializer, requestTracingOptions.Serializer);
            Assert.Equal(valueMaxStringLength, requestTracingOptions.ValueMaxStringLength);
        }

        [Fact]
        public void AddResponseTracing()
        {
            // act
            _mvcBuilder.AddResponseTracing();

            // assert
            var provider = _services.BuildServiceProvider();

            var responseTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcTracingOptions>>();
            Assert.NotNull(responseTracingOptions);

            var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
            Assert.Contains(mvcOptions.Filters,
                filter => filter is TypeFilterAttribute type &&
                          type.ImplementationType == typeof(AspNetMvcResponseTracingFilter));
        }

        [Fact]
        public void AddResponseTracing_Options()
        {
            // arrange
            var serializer = Mock.Of<ISerializer>();
            var valueMaxStringLength = 1000;

            // act
            _mvcBuilder.AddResponseTracing(
                tracing =>
                {
                    tracing.Serializer = serializer;
                    tracing.ValueMaxStringLength = valueMaxStringLength;
                });

            // assert
            var provider = _services.BuildServiceProvider();

            var responseTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcTracingOptions>>().Value;
            Assert.Same(serializer, responseTracingOptions.Serializer);
            Assert.Equal(valueMaxStringLength, responseTracingOptions.ValueMaxStringLength);
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
            var serializer = Mock.Of<ISerializer>();
            var valueMaxStringLength = 1000;

            // act
            _mvcBuilder.AddTracing(
                tracing =>
                {
                    tracing.Serializer = serializer;
                    tracing.ValueMaxStringLength = valueMaxStringLength;
                });

            // assert
            var provider = _services.BuildServiceProvider();

            var requestTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcTracingOptions>>().Value;
            Assert.Same(serializer, requestTracingOptions.Serializer);
            Assert.Equal(valueMaxStringLength, requestTracingOptions.ValueMaxStringLength);

            var responseTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcTracingOptions>>().Value;
            Assert.Same(serializer, responseTracingOptions.Serializer);
            Assert.Equal(valueMaxStringLength, responseTracingOptions.ValueMaxStringLength);
        }
    }
}