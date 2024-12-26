using System;
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

        public AspNetMvcRequestTracingFilter(
            ILoggerFactory loggerFactory,
            IOptions<AspNetMvcTracingOptions> options,
            IServiceProvider serviceProvider)
        {
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(options, nameof(serviceProvider));
            Guard.NotNull(loggerFactory, nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<AspNetMvcRequestTracingFilter>();
            _options = options.Value;
            _modelStateInvalidFilter = CreateModelStateFilter(serviceProvider);
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
            var requestContext = BuildRequestContext(context);

            await EnrichTraceWithTaggedRequestParams(activity, requestContext, cancellationToken);
            await EnrichTraceWithRequestEvent(activity, requestContext, cancellationToken);

            EnrichLogsWithHttpInfo(requestContext);
            EnrichLogsWithParams(requestContext.Parameters);
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

        private async Task EnrichTraceWithTaggedRequestParams(
            Activity? activity,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            if (activity is null || _options.EnrichTraceWithTaggedRequestParams == false)
                return;

            await foreach (var item in requestContext.EnumerateEventItemsAsync(_options, cancellationToken))
            {
                ActivityTagEnricher.Enrich(activity, item.Name, item.Value);
            }
        }

        private async Task EnrichTraceWithRequestEvent(
            Activity? activity,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            if (activity is null || _options.EnrichTraceWithRequestEvent == false)
                return;

            var tags = new ActivityTagsCollection();

            await foreach (var item in requestContext.EnumerateEventItemsAsync(_options, cancellationToken))
            {
                tags.Add(item.Name, item.Value);
            }

            var @event = new ActivityEvent("Action executing", tags: tags);
            activity.AddEvent(@event);
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

        private void EnrichLogsWithHttpInfo(RequestContext context)
        {
            if (_options.EnrichLogsWithHttpInfo == false)
                return;

            LogPropertyDataAccessor.AddTelemetryItem(HttpRequestKeys.Url, context.Url);
        }

        private void EnrichLogsWithParams(RequestContextParameter[] parameters)
        {
            if (_options.EnrichLogsWithParams == false)
                return;

            var telemetryItems = parameters
                .SelectMany(i => ObjectTelemetryItemsCollector.Collect(i.Name, i.Value, HttpRequestKeys.ParamsPrefix))
                .ToArray();

            if (_options.EnrichLogsWithParams)
                LogPropertyDataAccessor.AddTelemetryItems(telemetryItems);
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
                yield return new StructuredActivityEventItem(HttpRequestKeys.Headers.Accept, AcceptFormats);
                yield return new StructuredActivityEventItem(HttpRequestKeys.Headers.ContentType, ContentType);
                yield return new StructuredActivityEventItem(HttpRequestKeys.Headers.ContentLength, ContentLength);

                foreach (var parameter in Parameters)
                {
                    var json = await parameter.GetJson(options, cancellationToken);
                    yield return new StructuredActivityEventItem($"{HttpRequestKeys.ParamsPrefix}.{parameter.Value}", json);
                }
            }
        }

        private class RequestContextParameter(string name, object? value)
        {
            private string? _json;

            public string Name { get; } = name;

            public object? Value { get; } = value;

            public async Task<string?> GetJson(AspNetMvcTracingOptions tracingOptions, CancellationToken cancellationToken)
            {
                return _json ??= await tracingOptions.FormatAsync(Value, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private static ModelStateInvalidFilter CreateModelStateFilter(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<ApiBehaviorOptions>>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            return new ModelStateInvalidFilter(options.Value, loggerFactory.CreateLogger(typeof(ModelStateInvalidFilter)));
        }

        public int Order => _modelStateInvalidFilter.Order - 1;
    }
}