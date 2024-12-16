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
                }
            );

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

            var requestMessageString = GetBodyFromLogger(
                _loggerProvider
                    .Loggers[typeof(AspNetMvcRequestTracingFilter).FullName],
                RequestModelName
            );
            var responseMessageString = GetBodyFromLogger(
                _loggerProvider
                    .Loggers[typeof(AspNetMvcResponseTracingFilter).FullName],
                ResponseBodyName
            );
            
            var expected = JToken.FromObject(data).ToString(Formatting.None);

            // assert
            Assert.Equal(expected, requestMessageString, ignoreCase: true);
            Assert.Equal(expected, responseMessageString, ignoreCase: true);
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

            var requestMessageString = GetBodyFromLogger(
                _loggerProvider
                    .Loggers[typeof(AspNetMvcRequestTracingFilter).FullName],
                RequestModelName
            );
            var responseMessageString = GetBodyFromLogger(
                _loggerProvider
                    .Loggers[typeof(AspNetMvcResponseTracingFilter).FullName],
                ResponseBodyName
            );
            
            var expected = JObject.FromObject(data).ToString(Formatting.None);

            // assert
            Assert.Equal(expected, requestMessageString, ignoreCase: true);
            Assert.Equal(expected, responseMessageString, ignoreCase: true);
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
            Assert.Throws<HttpRequestException>(responseMessage.EnsureSuccessStatusCode);

            Assert.False(
                _loggerProvider
                    .Loggers
                    .ContainsKey(typeof(AspNetMvcRequestTracingFilter).FullName)
            );
            Assert.False(
                _loggerProvider
                    .Loggers
                    .ContainsKey(typeof(AspNetMvcResponseTracingFilter).FullName)
            );
        }

        private static string GetBodyFromLogger(TestLogger logger, string name)
        {
            var resMessageStr = logger.Messages[0].Split($"{name} = ")[1];
            return resMessageStr.Remove(resMessageStr.Length - 2);
        }
    }
}
