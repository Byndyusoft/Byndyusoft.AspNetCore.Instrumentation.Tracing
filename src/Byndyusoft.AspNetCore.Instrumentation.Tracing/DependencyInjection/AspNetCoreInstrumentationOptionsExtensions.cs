using Byndyusoft.AspNetCore.Instrumentation.Tracing.Enrichers;
using OpenTelemetry.Instrumentation.AspNetCore;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.DependencyInjection
{
    public static class AspNetCoreInstrumentationOptionsExtensions
    {
        public static HttpRequestEnricherBuilder BuildEnricher(this AspNetCoreInstrumentationOptions options)
        {
            var builder = new HttpRequestEnricherBuilder();
            options.EnrichWithHttpRequest += (activity, request) => builder.Enrich(activity, request);
            return builder;
        }

        public static HttpRequestEnricherBuilder BuildDefaultEnricher(this AspNetCoreInstrumentationOptions options)
        {
            return BuildEnricher(options)
                .WithDefaultConfiguration();
        }
    }
}