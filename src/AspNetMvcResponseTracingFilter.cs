using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public class AspNetMvcResponseTracingFilter : IAsyncResultFilter
    {
        private readonly AspNetMvcResponseTracingOptions _options;

        public AspNetMvcResponseTracingFilter(IOptions<AspNetMvcResponseTracingOptions> options)
        {
            Guard.NotNull(options, nameof(options));

            _options = options.Value;
        }

        public Task OnResultExecutionAsync(
            ResultExecutingContext context,
            ResultExecutionDelegate next)
        {
            return OnResultExecutionAsync(context, next, context.HttpContext.RequestAborted);
        }

        private async Task OnResultExecutionAsync(
            ResultExecutingContext context,
            ResultExecutionDelegate next,
            CancellationToken cancellationToken)
        {
            await next();

            if (Activity.Current == null)
                return;

            var tags = new ActivityTagsCollection
            {
                {"http.response.header.content_type", context.HttpContext.Response.ContentType},
                {"http.response.header.content_length", context.HttpContext.Response.ContentLength}
            };

            if (ActionResultBodyExtractor.TryExtractBody(context.Result, out var body))
            {
                var json = await _options.Serializer.SerializeResponseBodyAsync(body, _options, cancellationToken)
                    .ConfigureAwait(false);
                tags.Add("http.response.body", json);
            }

            var evnt = new ActivityEvent("Action executed", tags: tags);
            Activity.Current.AddEvent(evnt);
        }
    }
}