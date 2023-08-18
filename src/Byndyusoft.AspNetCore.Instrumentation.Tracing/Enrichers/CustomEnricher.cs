using System;
using System.Diagnostics;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Enrichers.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Enrichers
{
    public class CustomEnricher : IHttpRequestEnricher
    {
        private readonly Action<Activity, HttpRequest> _enrichAction;

        public CustomEnricher(Action<Activity, HttpRequest> enrichAction)
        {
            _enrichAction = enrichAction;
        }

        public void Enrich(Activity activity, HttpRequest httpRequest)
        {
            _enrichAction.Invoke(activity, httpRequest);
        }
    }
}