using System;
using System.Collections.Generic;
using System.Diagnostics;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Enrichers.Interfaces;
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

        public void Add(Action<Activity, HttpRequest> customEnrichAction)
        {
            Add(new CustomEnricher(customEnrichAction));
        }

        public void Enrich(Activity activity, HttpRequest httpRequest)
        {
            _httpRequestEnrichers.ForEach(enricher => enricher.Enrich(activity, httpRequest));
        }
    }
}