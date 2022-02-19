using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Unit
{
    public class TracingMvcBuilderExtensionsTests
    {
        private readonly IServiceCollection _services;
        private readonly IMvcBuilder _mvcBuilder;

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

            var requestTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcRequestTracingOptions>>();
            Assert.NotNull(requestTracingOptions);

            var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
            Assert.Contains(mvcOptions.Filters, filter => filter is TypeFilterAttribute type && type.ImplementationType == typeof(RequestTracingFilter));
        }

        [Fact]
        public void AddRequestTracing_Options()
        {
            // arrange
               var jsonSerializerOptions = new JsonSerializerOptions();
            var valueMaxStringLength = 1000;

            // act
            _mvcBuilder.AddRequestTracing(
                tracing =>
                {
                    tracing.JsonSerializerOptions = jsonSerializerOptions;
                    tracing.ValueMaxStringLength = valueMaxStringLength;
                });

            // assert
            var provider = _services.BuildServiceProvider();

            var requestTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcRequestTracingOptions>>().Value;
            Assert.Same(jsonSerializerOptions, requestTracingOptions.JsonSerializerOptions);
            Assert.Equal(valueMaxStringLength, requestTracingOptions.ValueMaxStringLength);
        }

        [Fact]
        public void AddResponseTracing()
        {
            // act
            _mvcBuilder.AddResponseTracing();

            // assert
            var provider = _services.BuildServiceProvider();
            
            var responseTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcResponseTracingOptions>>();
            Assert.NotNull(responseTracingOptions);

            var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
            Assert.Contains(mvcOptions.Filters, filter => filter is TypeFilterAttribute type && type.ImplementationType == typeof(ResponseTracingFilter));
        }

        [Fact]
        public void AddResponseTracing_Options()
        {
            // arrange
            var jsonSerializerOptions = new JsonSerializerOptions();
            var valueMaxStringLength = 1000;

            // act
            _mvcBuilder.AddResponseTracing(
                tracing =>
                {
                    tracing.JsonSerializerOptions = jsonSerializerOptions;
                    tracing.ValueMaxStringLength = valueMaxStringLength;
                });

            // assert
            var provider = _services.BuildServiceProvider();

            var responseTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcResponseTracingOptions>>().Value;
            Assert.Same(jsonSerializerOptions, responseTracingOptions.JsonSerializerOptions);
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

            var responseTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcResponseTracingOptions>>();
            Assert.NotNull(responseTracingOptions);

            var requestTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcRequestTracingOptions>>();
            Assert.NotNull(requestTracingOptions);

            Assert.Contains(mvcOptions.Filters, filter => filter is TypeFilterAttribute type && type.ImplementationType == typeof(ResponseTracingFilter));
            Assert.Contains(mvcOptions.Filters, filter => filter is TypeFilterAttribute type && type.ImplementationType == typeof(RequestTracingFilter));
        }

        [Fact]
        public void AddTracing_Options()
        {
            // arrange
            var jsonSerializerOptions = new JsonSerializerOptions();
            var valueMaxStringLength = 1000;

            // act
            _mvcBuilder.AddTracing(
                tracing =>
                {
                    tracing.JsonSerializerOptions = jsonSerializerOptions;
                    tracing.ValueMaxStringLength = valueMaxStringLength;
                });

            // assert
            var provider = _services.BuildServiceProvider();

            var requestTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcResponseTracingOptions>>().Value;
            Assert.Same(jsonSerializerOptions, requestTracingOptions.JsonSerializerOptions);
            Assert.Equal(valueMaxStringLength, requestTracingOptions.ValueMaxStringLength);

            var responseTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcRequestTracingOptions>>().Value;
            Assert.Same(jsonSerializerOptions, responseTracingOptions.JsonSerializerOptions);
            Assert.Equal(valueMaxStringLength, responseTracingOptions.ValueMaxStringLength);
        }

        //[Fact]
        //public void AddTracing_Options()
        //{
        //    // arrange
        //    var jsonSerializerOptions = new JsonSerializerOptions();
        //    var valueMaxStringLength = 1000;

        //    // act
        //    _mvcBuilder.AddTracing(tracing =>
        //    {
        //        tracing.JsonSerializerOptions = jsonSerializerOptions;
        //        tracing.ValueMaxStringLength = valueMaxStringLength;
        //    });

        //    // assert
        //    var provider = _services.BuildServiceProvider();

        //    var requestTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcRequestTracingOptions>>().Value;
        //    Assert.Same(jsonSerializerOptions, requestTracingOptions.JsonSerializerOptions);
        //    Assert.Same(valueMaxStringLength, requestTracingOptions.JsonSerializerOptions);



        //    Assert.NotNull(requestTracingOptions);
        //    Assert.Contains(mvcOptions.Filters, filter => filter is TypeFilterAttribute type && type.ImplementationType == typeof(RequestTracingFilter));

        //    var responseTracingOptions = provider.GetRequiredService<IOptions<AspNetMvcResponseTracingOptions>>();
        //    Assert.NotNull(responseTracingOptions);
        //    Assert.Contains(mvcOptions.Filters, filter => filter is TypeFilterAttribute type && type.ImplementationType == typeof(ResponseTracingFilter));
        //}
    }
}