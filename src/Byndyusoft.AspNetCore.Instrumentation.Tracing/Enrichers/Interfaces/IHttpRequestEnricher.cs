using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Enrichers.Interfaces
{
    public interface IHttpRequestEnricher
    {
        void Enrich(Activity activity, HttpRequest httpRequest);
    }
}