using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Enrichers
{
    public class HttpRequestEnricherManager
    {
        private readonly List<IHttpRequestEnricher> _httpRequestEnrichers = new();

        public void Add(IHttpRequestEnricher httpRequestEnricher)
        {
            _httpRequestEnrichers.Add(httpRequestEnricher);
        }

        public void Enrich(Activity activity, HttpRequest httpRequest)
        {
            _httpRequestEnrichers.ForEach(enricher => enricher.Enrich(activity, httpRequest));
        }
    }

    public class HttpRequestEnricherBuilder
    {
        private readonly HttpRequestEnricherManager _httpRequestEnricherManager;

        public HttpRequestEnricherBuilder(HttpRequestEnricherManager httpRequestEnricherManager)
        {
            _httpRequestEnricherManager = httpRequestEnricherManager;
        }

        public HttpRequestEnricherBuilder WithEnricher(
            IHttpRequestEnricher httpRequestEnricher)
        {
            _httpRequestEnricherManager.Add(httpRequestEnricher);
            return this;
        }

        public HttpRequestEnricherBuilder WithBuildConfiguration()
        {
            return WithEnricher(new BuildConfigurationEnricher());
        }
    }

    public static class AspNetCoreInstrumentationOptionsExtensions
    {
        public static AspNetCoreInstrumentationOptions
    }
}