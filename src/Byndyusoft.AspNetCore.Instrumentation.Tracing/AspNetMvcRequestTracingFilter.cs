using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Byndyusoft.Telemetry;
using Byndyusoft.Telemetry.Logging;
using Byndyusoft.Telemetry.OpenTelemetry;
using Microsoft.AspNetCore.Http.Extensions;
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
                EnrichLogsWithHttpInfo(requestContext);
                EnrichWithParams(activity, requestContext.Parameters);
                await LogRequestInTraceAsync(activity, requestContext, cancellationToken);
                await LogRequestInLogAsync(requestContext, cancellationToken);
            }

            await next();
        }

        private bool IsProcessingNeeded(Activity? activity)
        {
            if (_options.LogRequestInLog || _options.EnrichLogsWithParams || _options.EnrichLogsWithHttpInfo)
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
            await foreach (var formattedContextItem in context.GetFormattedItemsAsync(_options, cancellationToken))
            {
                var itemName = formattedContextItem.Name.Replace('.', '_');
                messageBuilder.Append($"{formattedContextItem.Description} = {{{itemName}}}; ");
                parameters.Add(formattedContextItem.FormattedValue);
            }

            _logger.LogInformation(messageBuilder.ToString(), parameters.ToArray());
        }

        private void EnrichLogsWithHttpInfo(
            RequestContext context)
        {
            if (_options.EnrichLogsWithHttpInfo == false)
                return;

            LogPropertyDataAccessor.AddTelemetryItem("http.request.url", context.Url);
        }

        private void EnrichWithParams(
            Activity? activity,
            RequestContextParameter[] requestContextParameters)
        {
            // TODO Перенести в DI или сделать static
            var collector = new ObjectTelemetryItemsCollector();

            if (_options.EnrichLogsWithParams == false &&
                (activity is null || _options.TagRequestParamsInTrace == false))
                return;

            var telemetryItems = requestContextParameters
                .SelectMany(i => collector.Collect(i.Name, i.Value, "http.request.params."))
                .ToArray();

            if (_options.EnrichLogsWithParams)
                LogPropertyDataAccessor.AddTelemetryItems(telemetryItems);

            if (_options.TagRequestParamsInTrace)
                ActivityTagEnricher.Enrich(telemetryItems);
        }

        private static RequestContext BuildRequestContext(ActionExecutingContext context)
        {
            var acceptFormats = context.HttpContext.Request.Headers["accept"].ToArray();
            var contentType = context.HttpContext.Request.ContentType;
            var contentLength = context.HttpContext.Request.ContentLength;
            var displayUrl = context.HttpContext.Request.GetDisplayUrl();

            return new RequestContext(
                acceptFormats,
                contentType,
                contentLength,
                GetParameters(context).ToArray(),
                displayUrl);
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
            public RequestContext(string[] acceptFormats,
                string? contentType,
                long? contentLength,
                RequestContextParameter[] parameters,
                string url)
            {
                AcceptFormats = acceptFormats;
                ContentType = contentType;
                ContentLength = contentLength;
                Parameters = parameters;
                Url = url;
            }

            public string[] AcceptFormats { get; }

            public string? ContentType { get; }

            public long? ContentLength { get; }

            public RequestContextParameter[] Parameters { get; }

            public string Url { get; }

            public async IAsyncEnumerable<FormattedContextItem> GetFormattedItemsAsync(
                AspNetMvcTracingOptions options,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                yield return new FormattedContextItem("http.request.header.accept", AcceptFormats, "Accept");
                yield return new FormattedContextItem("http.request.header.content_type", ContentType, "ContentType");
                yield return new FormattedContextItem("http.request.header.content_length", ContentLength,
                    "ContentLength");

                foreach (var parameter in Parameters)
                {
                    var json = await options.FormatAsync(parameter.Value, cancellationToken)
                        .ConfigureAwait(false);
                    yield return new FormattedContextItem($"http.request.params.{parameter.Name}", json,
                        parameter.Name);
                }
            }
        }

        private class RequestContextParameter
        {
            public RequestContextParameter(string name, object? value)
            {
                Name = name;
                Value = value;
            }

            public string Name { get; }

            public object? Value { get; }
        }
    }
}