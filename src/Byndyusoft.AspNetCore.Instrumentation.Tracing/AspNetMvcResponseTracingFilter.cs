using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Services;
using Byndyusoft.Logging;
using Byndyusoft.Logging.Extensions;
using Byndyusoft.Telemetry.Logging;
using Byndyusoft.Telemetry.OpenTelemetry;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public sealed class AspNetMvcResponseTracingFilter : IAsyncResourceFilter, IOrderedFilter
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

        public Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            return OnResourceExecutionAsync(next, context.HttpContext.RequestAborted);
        }

        private async Task OnResourceExecutionAsync(ResourceExecutionDelegate next, CancellationToken cancellationToken)
        {
            var activity = Activity.Current;

            LogPropertyDataAccessor.InitAsyncContext();

            var resourceExecutedContext = await next();

            var responseContext = BuildResponseContext(resourceExecutedContext);

            await EnrichTraceWithResponseEvent(activity, responseContext, cancellationToken);
            await EnrichTraceWithTaggedResponseParams(activity, responseContext, cancellationToken);

            await LogResponseInLogAsync(responseContext, cancellationToken);
        }

        private async Task EnrichTraceWithResponseEvent(
            Activity? activity,
            ResponseContext responseContext,
            CancellationToken cancellationToken)
        {
            if (activity is null || _options.EnrichTraceWithResponseEvent == false)
                return;

            var tags = new ActivityTagsCollection();
            await foreach (var item in responseContext.EnumerateEventItemsAsync(_options, cancellationToken))
            {
                tags.Add(item.Name, item.Value);
            }

            var @event = new ActivityEvent("Action executed", tags: tags);
            activity.AddEvent(@event);
        }

        private async Task EnrichTraceWithTaggedResponseParams(
            Activity? activity,
            ResponseContext responseContext,
            CancellationToken cancellationToken)
        {
            if (activity is null || _options.EnrichTraceWithTaggedResponseParams == false)
                return;

            await foreach (var item in responseContext.EnumerateEventItemsAsync(_options, cancellationToken))
            {
                ActivityTagEnricher.Enrich(activity, item.Name, item.Value);
            }
        }

        private async Task LogResponseInLogAsync(
            ResponseContext context,
            CancellationToken cancellationToken)
        {
            var eventItems = await context
                .EnumerateEventItemsAsync(_options, cancellationToken)
                .ToArrayAsync(cancellationToken);
            _logger.LogStructuredActivityEvent("Action executed", eventItems);
        }

        private static ResponseContext BuildResponseContext(ResourceExecutedContext context)
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
            public ResponseContext(
                string? contentType,
                long? contentLength,
                object? body)
            {
                ContentType = contentType;
                ContentLength = contentLength;
                Body = body;
            }

            public string? ContentType { get; }

            public long? ContentLength { get; }

            public object? Body { get; }

            public async IAsyncEnumerable<StructuredActivityEventItem> EnumerateEventItemsAsync(
                AspNetMvcTracingOptions options,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                yield return new StructuredActivityEventItem(HttpResponseKeys.Headers.ContentType, ContentType);
                yield return new StructuredActivityEventItem(HttpResponseKeys.Headers.ContentLength, ContentLength);

                var bodyJson = "<empty>";
                if (Body is not null)
                {
                    bodyJson = await options.FormatAsync(Body, cancellationToken)
                        .ConfigureAwait(false);
                }

                yield return new StructuredActivityEventItem(HttpResponseKeys.Body, bodyJson);
            }
        }

        public int Order => 3000;
    }
}