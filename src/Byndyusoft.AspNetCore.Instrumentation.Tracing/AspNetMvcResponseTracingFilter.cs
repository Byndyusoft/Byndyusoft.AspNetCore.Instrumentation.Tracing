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
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public class AspNetMvcResponseTracingFilter : IAsyncResourceFilter
    {
        private readonly ILogger<AspNetMvcResponseTracingFilter> _logger;
        private readonly AspNetMvcTracingOptions _options;

        private const string HeaderContentType = "http.response.header.content.type";
        private const string HeaderContentLength = "http.response.header.content.length";
        private const string BodyKey = "http.response.body";

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
            LogPropertyDataAccessor.InitAsyncContext();

            var resourceExecutedContext = await next();

            var responseContext = BuildResponseContext(resourceExecutedContext);
            await LogResponseInLogAsync(responseContext, cancellationToken);
        }

        private async Task LogResponseInLogAsync(
            ResponseContext context,
            CancellationToken cancellationToken)
        {
            if (_options.LogRequestInLog == false)
                return;

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
                string contentType,
                long? contentLength,
                object? body)
            {
                ContentType = contentType;
                ContentLength = contentLength;
                Body = body;
            }

            public string ContentType { get; }

            public long? ContentLength { get; }

            public object? Body { get; }

            public async IAsyncEnumerable<StructuredActivityEventItem> EnumerateEventItemsAsync(
                AspNetMvcTracingOptions options,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                yield return new StructuredActivityEventItem(HeaderContentType, ContentType);
                yield return new StructuredActivityEventItem(HeaderContentLength, ContentLength);

                var bodyJson = "<empty>";
                if (Body is not null)
                {
                    bodyJson = await options.FormatAsync(Body, cancellationToken)
                        .ConfigureAwait(false);
                }

                yield return new StructuredActivityEventItem(BodyKey, bodyJson);
            }
        }
    }
}