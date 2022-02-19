using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public class ResponseTracingFilter : IAsyncResultFilter
    {
        private static readonly object None = new();
        private readonly AspNetMvcResponseTracingOptions _options;
        private readonly ISerializer _serializer;

        public ResponseTracingFilter(IOptions<AspNetMvcResponseTracingOptions> options, ISerializer serializer)
        {
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(serializer, nameof(serializer));

            _serializer = serializer;
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

            if (TryGetBody(context.Result, out var body))
            {
                var json = await _serializer.SerializeResponseBodyAsync(body, _options, cancellationToken)
                    .ConfigureAwait(false);
                tags.Add("http.response.body", json);
            }

            var evnt = new ActivityEvent("Action executed", tags: tags);
            Activity.Current.AddEvent(evnt);
        }

        private static bool TryGetBody(IActionResult actionResult, out object? body)
        {
            body = actionResult switch
            {
                FileStreamResult _ => "stream",
                FileContentResult _ => "byte[]",
                ObjectResult objectResult => objectResult.Value,
                JsonResult jsonResult => jsonResult.Value,
                ContentResult contentResult => contentResult.Content,
                ViewResult viewResult => new {viewResult.Model, viewResult.ViewData, viewResult.TempData},
                _ => None
            };
            return body != None;
        }
    }
}