using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public class AspNetMvcRequestTracingFilter : IAsyncActionFilter
    {
        private readonly AspNetMvcTracingOptions _options;

        public AspNetMvcRequestTracingFilter(IOptions<AspNetMvcTracingOptions> options)
        {
            Guard.NotNull(options, nameof(options));

            _options = options.Value;
        }

        public Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            return OnActionExecutionAsync(context, next, context.HttpContext.RequestAborted);
        }

        private async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next,
            CancellationToken cancellationToken)
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
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var json = await _options.FormatAsync(value, cancellationToken)
                        .ConfigureAwait(false);
                    tags.Add($"http.request.params.{name}", json);
                }

                var @event = new ActivityEvent("Action executing", tags: tags);
                activity.AddEvent(@event);
            }

            await next();
        }

        private static IEnumerable<(string name, object? value)> GetParameters(ActionExecutingContext context)
        {
            foreach (var actionParameter in context.ActionDescriptor.Parameters)
            {
                if (actionParameter.BindingInfo?.BindingSource == BindingSource.Services ||
                    actionParameter.BindingInfo?.BindingSource == BindingSource.Special)
                    continue;

                var name = actionParameter.Name;
                if (context.ActionArguments.TryGetValue(actionParameter.Name, out var value))
                {
                    yield return (name, value);
                }
            }
        }
    }
}