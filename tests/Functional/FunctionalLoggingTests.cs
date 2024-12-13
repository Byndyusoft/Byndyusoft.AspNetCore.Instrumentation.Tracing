using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization.Json;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Xunit;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Functional
{
    public class FunctionalLoggingTests: MvcTestFixture
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions = new();
        private TestLoggerProvider? _loggerProvider;
        private const string RequestModelName = "http.request.params.model";
        private const string ResponseBodyName = "http.response.body";
        private const string RequestBodyName = "http.request.body";
        protected override void ConfigureMvc(IMvcCoreBuilder builder)
        {
            builder.AddTracing(
                tracing =>
                {
                    tracing.Formatter = new SystemTextJsonFormatter
                    {
                        Options = _jsonSerializerOptions
                    };
                });

            builder.Services
                .AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService("service"))
                .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation());
            _loggerProvider = (TestLoggerProvider)builder
                .Services
                .BuildServiceProvider()
                .GetService<ILoggerProvider>();
        }
        
        [Fact]
        public async Task Test_DefaultStringBehaviour_ExpectedRequestAndResponseLogging()
        {
            var data = "data";
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/msgpack"));
            var responseMessage = await Client.PostAsJsonAsync("test/echo",  data);
            responseMessage.EnsureSuccessStatusCode();

            var reqMessageStr =GetBodyFromLogger(
                _loggerProvider
                    .Loggers[typeof(AspNetMvcRequestTracingFilter).FullName],
                RequestModelName);
            var resMessageStr = GetBodyFromLogger(
                _loggerProvider
                    .Loggers[typeof(AspNetMvcResponseTracingFilter).FullName],
                ResponseBodyName);
            
            var expected = JToken.FromObject(data).ToString(Formatting.None);

            // assert
            Assert.Equal(expected, reqMessageStr, ignoreCase: true);
            Assert.Equal(expected, resMessageStr, ignoreCase: true);
        }
        
        [Fact]
        public async Task Test_DefaultJsonBehaviour_ExpectedRequestAndResponseLogging()
        {
            // arrange
            var data = new { Key = "key", Value = "value" };

            // act
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/msgpack"));
            var responseMessage = await Client.PostAsJsonAsync("test/echo", data);
            responseMessage.EnsureSuccessStatusCode();

            var reqMessageStr =GetBodyFromLogger(
                _loggerProvider
                    .Loggers[typeof(AspNetMvcRequestTracingFilter).FullName],
                RequestModelName);
            var resMessageStr = GetBodyFromLogger(
                _loggerProvider
                    .Loggers[typeof(AspNetMvcResponseTracingFilter).FullName],
                ResponseBodyName);
            
            var expected = JObject.FromObject(data).ToString(Formatting.None);

            // assert
            Assert.Equal(expected, reqMessageStr, ignoreCase: true);
            Assert.Equal(expected, resMessageStr, ignoreCase: true);
        }
        
        [Fact]
        public async Task Test_DefaultStreamBehaviour_ExpectedRequestAndResponseLogging()
        {
            var data = "data";
            using var dataMemoryStream = new MemoryStream();
            await using var dataStreamWriter = new StreamWriter(dataMemoryStream);
            await dataStreamWriter.WriteAsync(data);
            await dataStreamWriter.FlushAsync();
            dataMemoryStream.Position = 0;
            // act
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            
            var responseMessage = await Client.PostAsync("test/stream", new StreamContent(dataMemoryStream));
            responseMessage.EnsureSuccessStatusCode();

            var reqMessageStr =GetBodyFromLogger(
                _loggerProvider
                    .Loggers[typeof(AspNetMvcRequestTracingFilter).FullName],
                RequestBodyName);
            
            var resMessageStr = GetBodyFromLogger(
                _loggerProvider
                    .Loggers[typeof(AspNetMvcResponseTracingFilter).FullName],
                ResponseBodyName);

            // assert
            Assert.Equal(data, reqMessageStr, ignoreCase: true);
            //ActionResultBodyExtractor returns "stream" as response body if body is stream
            Assert.Equal("\"stream\"", resMessageStr, ignoreCase: true);
        }

        private static string GetBodyFromLogger(TestLogger logger, string name)
        {
            var resMessageStr = logger.Messages[0].Split($"{name} = ")[1];
            return resMessageStr.Remove(resMessageStr.Length - 2);
        }
    }
}
