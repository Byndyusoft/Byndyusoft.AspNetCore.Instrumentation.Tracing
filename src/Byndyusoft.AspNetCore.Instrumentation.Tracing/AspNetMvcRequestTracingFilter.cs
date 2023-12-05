using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            if (activity != null && IsProcessingNeeded())
            {
                var requestContext = BuildRequestContext(context);
                await LogRequestAsync(activity, requestContext, cancellationToken);
                await TagRequestParamsInTracesAsync(activity, requestContext.Parameters, cancellationToken);
            }

            await next();
        }

        private bool IsProcessingNeeded()
        {
            return _options.LogRequestInTraces || _options.TagRequestParamsInTraces;
        }

        private async Task LogRequestAsync(
            Activity activity,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            if (_options.LogRequestInTraces == false)
                return;

            var tags = new ActivityTagsCollection
            {
                { "http.request.header.accept", context.AcceptFormats },
                { "http.request.header.content_type", context.ContentType },
                { "http.request.header.content_length", context.ContentLength }
            };

            foreach (var parameter in context.Parameters)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var json = await _options.FormatAsync(parameter.Value, cancellationToken)
                    .ConfigureAwait(false);
                tags.Add($"http.request.params.{parameter.Name}", json);
            }

            var @event = new ActivityEvent("Action executing", tags: tags);
            activity.AddEvent(@event);
        }

        private async Task TagRequestParamsInTracesAsync(
            Activity activity,
            RequestContextParameter[] requestContextParameters,
            CancellationToken cancellationToken)
        {
            if (_options.TagRequestParamsInTraces == false)
                return;

            // TODO Нужно добавить тэгирование части простых свойств
            throw new InvalidOperationException();
        }

        private static RequestContext BuildRequestContext(ActionExecutingContext context)
        {
            var acceptFormats = context.HttpContext.Request.Headers["accept"].ToArray();
            var contentType = context.HttpContext.Request.ContentType;
            var contentLength = context.HttpContext.Request.ContentLength;

            return new RequestContext(
                acceptFormats,
                contentType,
                contentLength,
                GetParameters(context).ToArray());
        }

        private static IEnumerable<RequestContextParameter> GetParameters(ActionExecutingContext context)
        {
            foreach (var actionParameter in context.ActionDescriptor.Parameters)
            {
                if (actionParameter.BindingInfo?.BindingSource == BindingSource.Services ||
                    actionParameter.BindingInfo?.BindingSource == BindingSource.Special)
                    continue;

                var name = actionParameter.Name;
                if (context.ActionArguments.TryGetValue(actionParameter.Name, out var value))
                {
                    yield return new RequestContextParameter(name, value);
                }
            }
        }

        private class RequestContext
        {
            public string[] AcceptFormats { get; }

            public string ContentType { get; }

            public long? ContentLength { get; }

            public RequestContextParameter[] Parameters { get; }

            public RequestContext(
                string[] acceptFormats,
                string contentType,
                long? contentLength,
                RequestContextParameter[] parameters)
            {
                AcceptFormats = acceptFormats;
                ContentType = contentType;
                ContentLength = contentLength;
                Parameters = parameters;
            }
        }

        private class RequestContextParameter
        {
            public string Name { get; }

            public object? Value { get; }

            public RequestContextParameter(string name, object? value)
            {
                Name = name;
                Value = value;
            }
        }
    }
}