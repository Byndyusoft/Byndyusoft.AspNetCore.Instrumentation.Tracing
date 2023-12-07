using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public class AspNetMvcResponseTracingFilter : IAsyncResultFilter
    {
        private readonly ILogger<AspNetMvcResponseTracingFilter> _logger;
        private readonly AspNetMvcTracingOptions _options;

        public AspNetMvcResponseTracingFilter(
            ILogger<AspNetMvcResponseTracingFilter> logger,
            IOptions<AspNetMvcTracingOptions> options)
        {
            _logger = logger;
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

            var activity = Activity.Current;
            if (IsProcessingNeeded(activity) == false)
                return;

            var responseContext = BuildResponseContext(context);
            await LogResponseInTraceAsync(activity, responseContext, cancellationToken);
            await LogResponseInLogAsync(responseContext, cancellationToken);
        }

        private async Task LogResponseInTraceAsync(
            Activity? activity,
            ResponseContext context,
            CancellationToken cancellationToken)
        {
            if (activity is null || _options.LogResponseInTrace == false)
                return;

            var tags = new ActivityTagsCollection();
            await foreach (var telemetryItem in context.GetFormattedItemsAsync(_options, cancellationToken))
            {
                tags.Add(telemetryItem.Name, telemetryItem.FormattedValue);
            }

            var @event = new ActivityEvent("Action executed", tags: tags);
            activity.AddEvent(@event);
        }

        private async Task LogResponseInLogAsync(
            ResponseContext context,
            CancellationToken cancellationToken)
        {
            if (_options.LogRequestInLog == false)
                return;

            var messageBuilder = new StringBuilder("Action executed: ");
            var parameters = new List<object?>();
            await foreach (var telemetryItem in context.GetFormattedItemsAsync(_options, cancellationToken))
            {
                messageBuilder.Append($"{telemetryItem.Description} = {{{telemetryItem.Name}}}; ");
                parameters.Add(telemetryItem.FormattedValue);
            }

            _logger.LogInformation(messageBuilder.ToString(), parameters.ToArray());
        }

        private bool IsProcessingNeeded(Activity? activity)
        {
            if (_options.LogResponseInLog)
                return true;

            return activity is not null && _options.LogResponseInTrace;
        }

        private static ResponseContext BuildResponseContext(ResultExecutingContext context)
        {
            var contentType = context.HttpContext.Response.ContentType;
            var contentLength = context.HttpContext.Response.ContentLength;
            if (ActionResultBodyExtractor.TryExtractBody(context.Result, out var body) == false)
                body = null;

            return new ResponseContext(
                contentType,
                contentLength,
                body);
        }

        private class ResponseContext
        {
            public string ContentType { get; }

            public long? ContentLength { get; }

            public object? Body { get; }

            public ResponseContext(
                string contentType,
                long? contentLength,
                object? body)
            {
                ContentType = contentType;
                ContentLength = contentLength;
                Body = body;
            }

            public async IAsyncEnumerable<FormattedContextItem> GetFormattedItemsAsync(
                AspNetMvcTracingOptions options,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                yield return new FormattedContextItem("http.response.header.content_type", ContentType, "ContentType");
                yield return new FormattedContextItem("http.response.header.content_length", ContentLength, "ContentLength");

                var bodyJson = "<empty>";
                if (Body is not null)
                {
                    bodyJson = await options.FormatAsync(Body, cancellationToken)
                        .ConfigureAwait(false);
                }
                yield return new FormattedContextItem("http.response.body", bodyJson, "Body");
            }
        }
    }
}