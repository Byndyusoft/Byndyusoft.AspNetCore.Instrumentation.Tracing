using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public class AspNetMvcRequestTracingFilter : IAsyncActionFilter
    {
        private readonly ILogger<AspNetMvcRequestTracingFilter> _logger;
        private readonly AspNetMvcTracingOptions _options;

        public AspNetMvcRequestTracingFilter(
            ILogger<AspNetMvcRequestTracingFilter> logger,
            IOptions<AspNetMvcTracingOptions> options)
        {
            _logger = logger;
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
            if (IsProcessingNeeded(activity))
            {
                var requestContext = BuildRequestContext(context);
                await LogRequestInTraceAsync(activity, requestContext, cancellationToken);
                await LogRequestInLogAsync(requestContext, cancellationToken);
                await TagRequestParamsInTracesAsync(activity, requestContext.Parameters, cancellationToken);
            }

            await next();
        }

        private bool IsProcessingNeeded(Activity? activity)
        {
            if (_options.LogRequestInLog)
                return true;

            return activity is not null && (_options.LogRequestInTrace || _options.TagRequestParamsInTrace);
        }

        private async Task LogRequestInTraceAsync(
            Activity? activity,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            if (activity is null || _options.LogRequestInTrace == false)
                return;

            var tags = new ActivityTagsCollection();
            await foreach (var telemetryItem in context.GetFormattedItemsAsync(_options, cancellationToken))
            {
                tags.Add(telemetryItem.Name, telemetryItem.FormattedValue);
            }

            var @event = new ActivityEvent("Action executing", tags: tags);
            activity.AddEvent(@event);
        }

        private async Task LogRequestInLogAsync(
            RequestContext context,
            CancellationToken cancellationToken)
        {
            if (_options.LogRequestInLog == false)
                return;

            var messageBuilder = new StringBuilder("Action executing: ");
            var parameters = new List<object?>();
            await foreach (var telemetryItem in context.GetFormattedItemsAsync(_options, cancellationToken))
            {
                messageBuilder.Append($"{telemetryItem.Description} = {{{telemetryItem.Name}}}; ");
                parameters.Add(telemetryItem.FormattedValue);
            }

            _logger.LogInformation(messageBuilder.ToString(), parameters.ToArray());
        }

        private async Task TagRequestParamsInTracesAsync(
            Activity? activity,
            RequestContextParameter[] requestContextParameters,
            CancellationToken cancellationToken)
        {
            if (activity is null || _options.TagRequestParamsInTrace == false)
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

            public async IAsyncEnumerable<FormattedContextItem> GetFormattedItemsAsync(
                AspNetMvcTracingOptions options, 
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                yield return new FormattedContextItem("http.request.header.accept", AcceptFormats, "Accept");
                yield return new FormattedContextItem("http.request.header.content_type", ContentType, "ContentType");
                yield return new FormattedContextItem("http.request.header.content_length", ContentLength, "ContentLength");

                foreach (var parameter in Parameters)
                {
                    var json = await options.FormatAsync(parameter.Value, cancellationToken)
                        .ConfigureAwait(false);
                    yield return new FormattedContextItem($"http.request.params.{parameter.Name}", json, parameter.Name);
                }
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