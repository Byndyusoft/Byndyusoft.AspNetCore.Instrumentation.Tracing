using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Utility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Unit
{
    public class RequestBodyLoggingMiddlewareTests:IDisposable
    {
        private readonly IHost _host;
        private readonly TestLogger _logger;
        public RequestBodyLoggingMiddlewareTests()
        {
            _host = new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices(services =>
                        {
                            services.AddSingleton<ConsumedMessageLoggingMiddleware>();
                            services.AddLogging(loggingBuilder =>
                            {
                                loggingBuilder.Services
                                    .AddSingleton<ILoggerProvider, TestLoggerProvider>();
                            });
                        })
                        .Configure(app =>
                        {
                            app.UseMiddleware<ConsumedMessageLoggingMiddleware>();
                        });
                })
                .Start();
            var loggerProvider = (TestLoggerProvider)_host
                .Services
                .GetService<ILoggerProvider>();
            
            _logger = loggerProvider.Loggers[typeof(ConsumedMessageLoggingMiddleware).FullName];
        }
        
        [Fact]
        public async Task MiddlewareTest_LogStringMessageBody()
        {
            var data = "data";
            await _host.GetTestClient().PostAsync(String.Empty, new StringContent(data));
            var messageStr = _logger.Messages[0];
            var message =
                JObject
                    .Parse(messageStr)["MessageBody"]!
                    .ToString();
            Assert.Equal(data, message);
        }
        
        [Fact]
        public async Task MiddlewareTest_LogJObjectMessageBody()
        {
            var dataJObj = JObject.FromObject(new{Name="name", Address = "address"});
            await _host.GetTestClient().PostAsync(String.Empty, new StringContent(dataJObj.ToString()));
            var messageStr = _logger.Messages[0];
            //В тело запроса могут передаваться разные типы,
            //поэтому данные дополнительно оборачиваются в строку
            var messageJObj = JObject
                .Parse
                (
                    JObject
                        .Parse(messageStr)["MessageBody"]!
                        .ToString()
                );
            Assert.True(JToken.DeepEquals(dataJObj, messageJObj));
        }
        
        [Fact]
        public async Task MiddlewareTest_LogStreamMessageBody()
        {
            var data = "data";
            using var dataMemoryStream = new MemoryStream();
            await using var dataStreamWriter = new StreamWriter(dataMemoryStream);
            await dataStreamWriter.WriteAsync(data);
            await dataStreamWriter.FlushAsync();
            dataMemoryStream.Position = 0;
            await _host.GetTestClient().PostAsync(String.Empty, new StreamContent(dataMemoryStream));
            var messageStr = _logger.Messages[0];
            var message =
                JObject
                    .Parse(messageStr)["MessageBody"]!
                    .ToString();
            Assert.Equal(data, message);
        }

        public void Dispose()
        {
            _host.Dispose();
        }
    }
}
