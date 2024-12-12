using System;
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

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Functional
{
    public class FunctionalTests : MvcTestFixture
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions = new();
        private Activity? _activity;
        private Action<AspNetMvcTracingOptions>? _configureTest;

        private const string ContentTypeHeaderKey = "http.request.header.content.type";
        private const string RequestModelKey = "http.request.params.model";
        private const string AcceptHeaderKey = "http.request.header.accept";
        private const string ResponseBodyKey = "http.response.body";
        private const string ContentTypeHeaderValue = "application/json; charset=utf-8";
        private const string EventActionExecuted = "Action executed";
        private const string EventActionExecuting = "Action executing";

        public FunctionalTests()
        {
            _jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower; 
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
                    _configureTest?.Invoke(tracing);
                });

            builder.Services
                .AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService("service"))
                .WithTracing(
                    tracing =>
                    {
                        tracing
                            .AddAspNetCoreInstrumentation(
                                options =>
                                {
                                    options.EnrichWithHttpRequest +=
                                        (activity, _) => _activity = activity;
                                }
                            );
                    }
                );
        }

        [Fact]
        public async Task Test_DefaultBehaviour_ExpectedRequestAndResponseEvents()
        {
            // arrange
            var param = new { Key = "key", Value = "value" };

            // act
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/msgpack"));
            var responseMessage = await Client.PostAsJsonAsync("test/echo", param);
            responseMessage.EnsureSuccessStatusCode();

            // assert
            Assert.NotNull(_activity);
            var activity = _activity!;

            AssertRequestEventDoesNotExist(activity);
            AssertResponseEventDoesNotExist(activity);
        }

        [Fact]
        public async Task Test_LogRequestIsTurnedOn_ExpectedRequestEvent()
        {
            // arrange
            _configureTest = options => options.LogRequestInTrace = true;
            var param = new { Key = "key", Value = "value" };

            // act
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/msgpack"));
            var responseMessage = await Client.PostAsJsonAsync("test/echo", param);
            responseMessage.EnsureSuccessStatusCode();

            // assert
            Assert.NotNull(_activity);
            var activity = _activity!;

            AssertRequestEvent(activity, param);
            AssertResponseEventDoesNotExist(activity);
        }

        private void AssertResponseEvent(Activity activity, object param)
        {
            var response = Assert
                .Single(
                    activity.Events,
                    @event =>
                        @event.Name == EventActionExecuted
                );

            var contentTypeHeader = Assert
                .Single(
                    response.Tags,
                    tag => 
                        tag.Key == ContentTypeHeaderKey
                );
            Assert.Equal(ContentTypeHeaderValue, contentTypeHeader.Value);

            var body = Assert
                .Single(
                    response.Tags,
                    tag =>
                        tag.Key == ResponseBodyKey
                );
            Assert.Equal(
                JsonSerializer
                    .Serialize(param, _jsonSerializerOptions),
                body.Value
            );
        }

        private void AssertResponseEventDoesNotExist(Activity activity)
        {
            Assert.DoesNotContain(
                activity.Events,
                @event => @event.Name == EventActionExecuted
            );
        }

        private void AssertRequestEvent(Activity activity, object param)
        {
            var request = Assert
                .Single(
                    activity.Events,
                    @event =>
                        @event.Name == EventActionExecuting
                );

            var acceptHeader = Assert
                .Single(
                    request.Tags,
                    tag => tag.Key == AcceptHeaderKey
                );
            Assert.Equal(
                new[]
                {
                    "application/json, application/msgpack"
                },
                acceptHeader.Value
            );

            var contentTypeHeader =  Assert
                .Single(
                    request.Tags,
                    tag => tag.Key == ContentTypeHeaderKey
                );
            Assert.Equal(
                ContentTypeHeaderValue,
                contentTypeHeader.Value
            );

            var model = Assert
                .Single(
                    request.Tags,
                    tag =>
                        tag.Key == RequestModelKey
                );
            Assert.Equal(
                JsonSerializer.Serialize(param, _jsonSerializerOptions),
                model.Value
            );
        }

        private void AssertRequestEventDoesNotExist(Activity activity)
        {
            Assert.DoesNotContain(
                activity.Events,
                @event =>
                    @event.Name == EventActionExecuting
            );
        }
    }
}