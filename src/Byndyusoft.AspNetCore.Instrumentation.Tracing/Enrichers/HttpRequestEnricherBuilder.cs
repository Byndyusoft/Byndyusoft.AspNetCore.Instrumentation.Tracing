using System;
using System.Diagnostics;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Enrichers.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Enrichers
{
    public class HttpRequestEnricherBuilder
    {
        private readonly HttpRequestEnricherManager _httpRequestEnricherManager;

        public HttpRequestEnricherBuilder()
        {
            _httpRequestEnricherManager = new HttpRequestEnricherManager();
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

        public HttpRequestEnricherBuilder WithDefaultConfiguration()
        {
            return WithBuildConfiguration();
        }

        public HttpRequestEnricherBuilder WithCustomEnrichAction(Action<Activity, HttpRequest> customEnrichAction)
        {
            _httpRequestEnricherManager.Add(customEnrichAction);
            return this;
        }

        public void Enrich(Activity activity, HttpRequest httpRequest)
        {
            _httpRequestEnricherManager.Enrich(activity, httpRequest);
        }
    }
}