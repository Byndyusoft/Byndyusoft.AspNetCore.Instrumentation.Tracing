using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public class AspNetMvcResponseTracingFilter : IAsyncActionFilter
    {
        private readonly AspNetMvcTracingOptions _options;

        public AspNetMvcResponseTracingFilter(IOptions<AspNetMvcTracingOptions> options)
        {
            Guard.NotNull(options, nameof(options));

            _options = options.Value;
        }

        public Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            return OnActionExecutionAsync(next, context.HttpContext.RequestAborted);
        }

        private async Task OnActionExecutionAsync(
            ActionExecutionDelegate next,
            CancellationToken cancellationToken)
        {
            var actionExecutedContext = await next();

            if (Activity.Current == null)
                return;

            var tags = new ActivityTagsCollection
            {
                {"http.response.header.content_type", actionExecutedContext.HttpContext.Response.ContentType},
                {"http.response.header.content_length", actionExecutedContext.HttpContext.Response.ContentLength}
            };

            if (ActionResultBodyExtractor.TryExtractBody(actionExecutedContext.Result, out var body))
            {
                var json = await _options.FormatAsync(body, cancellationToken)
                    .ConfigureAwait(false);
                tags.Add("http.response.body", json);
            }

            var evnt = new ActivityEvent("Action executed", tags: tags);
            Activity.Current.AddEvent(evnt);
        }
    }
}