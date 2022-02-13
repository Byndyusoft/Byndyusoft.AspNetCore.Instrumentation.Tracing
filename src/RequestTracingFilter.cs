using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public class RequestTracingFilter : IAsyncActionFilter
    {
        private readonly AspNetMvcRequestTracingOptions _options;

        public RequestTracingFilter(IOptions<AspNetMvcRequestTracingOptions> options)
        {
            _options = options.Value;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                var tags = new ActivityTagsCollection
                {
                    {"http.request.header.accept", context.HttpContext.Request.Headers["accept"].ToArray()},
                    {"http.request.header.content_type", context.HttpContext.Request.ContentType},
                    {"http.request.header.content_length", context.HttpContext.Request.ContentLength}
                };

                foreach ((string name, var value) in GetParameters(context))
                    tags.Add($"http.request.params.{name}",
                        JsonSerializer.Serialize(value, _options.JsonSerializerOptions));

                var evnt = new ActivityEvent("Action executing", tags: tags);
                activity.AddEvent(evnt);
            }

            await next();
        }

        private static IEnumerable<(string name, object? value)> GetParameters(ActionExecutingContext context)
        {
            foreach (var actionParameter in context.ActionDescriptor.Parameters)
            {
                if (actionParameter.BindingInfo.BindingSource == BindingSource.Services ||
                    actionParameter.BindingInfo.BindingSource == BindingSource.Special)
                    continue;

                var name = actionParameter.Name;
                var value = context.ActionArguments[actionParameter.Name];

                yield return (name, value);
            }
        }
    }
}