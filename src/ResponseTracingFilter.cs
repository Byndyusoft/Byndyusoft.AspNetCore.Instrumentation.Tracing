using System.Diagnostics;
using System.Text.Json;
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

        public ResponseTracingFilter(IOptions<AspNetMvcResponseTracingOptions> options)
        {
            _options = options.Value;
        }

        public async Task OnResultExecutionAsync(
            ResultExecutingContext context,
            ResultExecutionDelegate next)
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
                tags.Add("http.response.body", JsonSerializer.Serialize(body, _options.JsonSerializerOptions));

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