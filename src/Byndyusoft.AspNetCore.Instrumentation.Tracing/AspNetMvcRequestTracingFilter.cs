using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Byndyusoft.Logging;
using Byndyusoft.Logging.Extensions;
using Byndyusoft.Telemetry;
using Byndyusoft.Telemetry.Logging;
using Byndyusoft.Telemetry.OpenTelemetry;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    /// <see href="https://github.com/dotnet/aspnetcore/issues/50432"/>
    /// <remarks>
    ///     Т.к. пользовательские фильтры всегда вызываются после системных,
    ///     то в случае включения валидации фильтр <see cref="ModelStateInvalidFilter"/> отрабатывает раньше
    ///     и до логирования проблемного запроса дело не доходит.
    ///     Из-за этого мы вынуждены вызывать <see cref="ModelStateInvalidFilter"/> вручную
    ///     и отключать автоматическую проверку в <see cref="TracingMvcBuilderExtensions.PostConfigureMvcOptions"/> 
    /// </remarks>>
    public sealed class AspNetMvcRequestTracingFilter : IAsyncActionFilter, IOrderedFilter
    {
        private readonly ILogger<AspNetMvcRequestTracingFilter> _logger;
        private readonly AspNetMvcTracingOptions _options;
        private readonly ModelStateInvalidFilter _modelStateInvalidFilter;
        
        private const string AcceptHeader = "http.request.header.accept";
        private const string ContentTypeHeader = "http.request.header.content_type";
        private const string ContentLengthHeader = "http.request.header.content_length";

        public AspNetMvcRequestTracingFilter(
            ILoggerFactory loggerFactory,
            IOptions<AspNetMvcTracingOptions> options,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
        {
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(apiBehaviorOptions, nameof(apiBehaviorOptions));
            Guard.NotNull(loggerFactory, nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<AspNetMvcRequestTracingFilter>();
            _options = options.Value;
            _modelStateInvalidFilter = new ModelStateInvalidFilter(
                apiBehaviorOptions.Value,
                loggerFactory.CreateLogger(typeof(ModelStateInvalidFilter)));
        }

        public Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next
        )
        {
            return OnActionExecutionAsync(context, next, context.HttpContext.RequestAborted);
        }

        private async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next,
            CancellationToken cancellationToken)
        {
            var activity = Activity.Current;
            var requestContext = BuildRequestContext(context);
            EnrichLogsWithHttpInfo(requestContext);
            EnrichWithParams(activity, requestContext.Parameters);
            await LogRequestInLogAsync(requestContext, cancellationToken);

            if (_options.InitialSuppressModelStateInvalidFilter == false)
            {
                _modelStateInvalidFilter.OnActionExecuting(context);
            }

            if (context.Result is null)
            {
                await next();
            }
        }

        private async Task LogRequestInLogAsync(
            RequestContext context,
            CancellationToken cancellationToken
        )
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
            RequestContextParameter[] requestContextParameters
        )
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

        private static RequestContext BuildRequestContext(ActionExecutingContext context)
        {
            var acceptFormats = context.HttpContext.Request.Headers["accept"].ToArray();
            var contentType = context.HttpContext.Request.ContentType;
            var contentLength = context.HttpContext.Request.ContentLength;
            var displayUrl = context.HttpContext.Request.GetDisplayUrl();
            var parameters = GetParameters(context).ToArray();

            return new RequestContext(
                acceptFormats,
                contentType,
                contentLength,
                parameters,
                displayUrl
            );
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
            public RequestContext(
                string[]? acceptFormats,
                string? contentType,
                long? contentLength,
                RequestContextParameter[] parameters,
                string url
            )
            {
                AcceptFormats = acceptFormats;
                ContentType = contentType;
                ContentLength = contentLength;
                Parameters = parameters;
                Url = url;
            }

            public string[]? AcceptFormats { get; }

            public string? ContentType { get; }

            public long? ContentLength { get; }

            public RequestContextParameter[] Parameters { get; }

            public string Url { get; }

            public async IAsyncEnumerable<StructuredActivityEventItem> EnumerateEventItemsAsync(
                AspNetMvcTracingOptions options,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                yield return new StructuredActivityEventItem(AcceptHeader, AcceptFormats);
                yield return new StructuredActivityEventItem(ContentTypeHeader, ContentType);
                yield return new StructuredActivityEventItem(ContentLengthHeader, ContentLength);

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

        public int Order => _modelStateInvalidFilter.Order - 1;
    }
}