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
    public class FunctionalTracingTests : MvcTestFixture
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions = new();
        private Activity? _activity;
        private Action<AspNetMvcTracingOptions>? _configureTest;
        
        private const string EventActionExecuted = "Action executed";
        private const string EventActionExecuting = "Action executing";

        public FunctionalTracingTests()
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
                                    options.EnrichWithHttpRequest = (activity, _) => _activity = activity
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

        private void AssertResponseEventDoesNotExist(Activity activity)
        {
            Assert.DoesNotContain(
                activity.Events,
                @event => @event.Name == EventActionExecuted
            );
        }

        private void AssertRequestEventDoesNotExist(Activity activity)
        {
            Assert.DoesNotContain(
                activity.Events,
                @event => @event.Name == EventActionExecuting
            );
        }
    }
}