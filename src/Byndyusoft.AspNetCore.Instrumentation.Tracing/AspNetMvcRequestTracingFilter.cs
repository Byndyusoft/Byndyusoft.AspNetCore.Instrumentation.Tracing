using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Byndyusoft.Logging;
using Byndyusoft.Logging.Extensions;
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
        
        private const string AcceptHeader = "http.request.header.accept";
        private const string ContentTypeHeader = "http.request.header.content.type";
        private const string ContentLengthHeader = "http.request.header.content.length";
        private const string BodyHeader = "http.request.body";

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
            var requestContext = await BuildRequestContext(context);
            EnrichLogsWithHttpInfo(requestContext);
            EnrichWithParams(activity, requestContext.Parameters);
            await LogRequestInLogAsync(requestContext, cancellationToken);
            await next();
        }

        private async Task LogRequestInLogAsync(
            RequestContext context,
            CancellationToken cancellationToken)
        {
            var eventItems = await context
                .EnumerateEventItemsAsync(_options, cancellationToken)
                .ToArrayAsync(cancellationToken);
            _logger.LogStructuredActivityEvent("Action executing", eventItems);
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
            if (_options.EnrichLogsWithParams == false
                && activity is null
                && _options.EnrichTraceWithTaggedRequestParams == false)
                return;

            var telemetryItems = requestContextParameters
                .SelectMany(i => ObjectTelemetryItemsCollector.Collect(i.Name, i.Value, "http.request.params."))
                .ToArray();

            if (_options.EnrichLogsWithParams)
                LogPropertyDataAccessor.AddTelemetryItems(telemetryItems);

            if (activity is not null && _options.EnrichTraceWithTaggedRequestParams)
                ActivityTagEnricher.Enrich(activity, telemetryItems);
        }

        private static async Task<RequestContext> BuildRequestContext(
            ActionExecutingContext context)
        {
            var acceptFormats = context.HttpContext.Request.Headers["accept"].ToArray();
            var contentType = context.HttpContext.Request.ContentType;
            var contentLength = context.HttpContext.Request.ContentLength;
            var displayUrl = context.HttpContext.Request.GetDisplayUrl();
            var parameters = GetParameters(context).ToArray();
            var body = "";
            if (contentLength.HasValue && !parameters.Any(_ => _.Name.Equals("model")))
            {
                using var reader = new StreamReader(
                    context.HttpContext.Request.Body,
                    Encoding.UTF8);
                body = await reader.ReadToEndAsync();
            }

            return new RequestContext(
                acceptFormats,
                contentType,
                contentLength,
                parameters,
                displayUrl,
                body);
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
                string url,
                string body)
            {
                AcceptFormats = acceptFormats;
                ContentType = contentType;
                ContentLength = contentLength;
                Parameters = parameters;
                Url = url;
                Body = body;
            }

            public string[] AcceptFormats { get; }

            public string? ContentType { get; }

            public long? ContentLength { get; }

            public RequestContextParameter[] Parameters { get; }

            public string Url { get; }
            public string Body { get; }

            public async IAsyncEnumerable<StructuredActivityEventItem> EnumerateEventItemsAsync(
                AspNetMvcTracingOptions options,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                yield return new StructuredActivityEventItem(AcceptHeader, AcceptFormats);
                yield return new StructuredActivityEventItem(ContentTypeHeader, ContentType);
                yield return new StructuredActivityEventItem(ContentLengthHeader, ContentLength);
                yield return new StructuredActivityEventItem(BodyHeader, Body);

                foreach (var parameter in Parameters)
                {
                    var json = await options.FormatAsync(parameter.Value, cancellationToken)
                        .ConfigureAwait(false);
                    yield return new StructuredActivityEventItem($"http.request.params.{parameter.Name}", json);
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