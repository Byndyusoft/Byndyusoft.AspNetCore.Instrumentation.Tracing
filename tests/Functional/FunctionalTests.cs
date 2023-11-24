using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization.Json;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Xunit;
using Yoh.Text.Json.NamingPolicies;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Functional
{
    public class FunctionalTests : MvcTestFixture
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions = new();
        private Activity? _activity;

        public FunctionalTests()
        {
            _jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicies.SnakeCaseLower;
        }

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

            builder.Services.AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("service"))
                        .AddAspNetCoreInstrumentation(
                            options =>
                            {
                                options.EnrichWithHttpRequest += (activity, _) => _activity = activity;
                            });
                });
        }

        [Fact]
        public async Task Test()
        {
            // arrange
            var param = new {Key = "key", Value = "value"};

            // act
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/msgpack"));
            var responseMessage = await Client.PostAsJsonAsync("test/echo", param);
            responseMessage.EnsureSuccessStatusCode();
            
            // assert
            Assert.NotNull(_activity);
            var activity = _activity!;

            {
                var request = Assert.Single(activity.Events, evnt => evnt.Name == "Action executing");

                var acceptHeader = Assert.Single(request.Tags, tag => tag.Key == "http.request.header.accept");
                Assert.Equal(new[] {"application/json, application/msgpack"}, acceptHeader.Value);

                var contentTypeHeader =
                    Assert.Single(request.Tags, tag => tag.Key == "http.request.header.content_type");
                Assert.Equal("application/json; charset=utf-8", contentTypeHeader.Value);

                var model = Assert.Single(request.Tags, tag => tag.Key == "http.request.params.model");
                Assert.Equal(JsonSerializer.Serialize(param, _jsonSerializerOptions), model.Value);
            }

            {
                var response = Assert.Single(activity.Events, evnt => evnt.Name == "Action executed");

                var contentTypeHeader =
                    Assert.Single(response.Tags, tag => tag.Key == "http.response.header.content_type");
                Assert.Equal("application/json; charset=utf-8", contentTypeHeader.Value);

                var body = Assert.Single(response.Tags, tag => tag.Key == "http.response.body");
                Assert.Equal(JsonSerializer.Serialize(param, _jsonSerializerOptions), body.Value);
            }
        }
    }
}