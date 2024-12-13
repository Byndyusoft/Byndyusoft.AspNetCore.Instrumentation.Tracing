using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
    public class AspNetMvcRequestTracingFilter : IAsyncActionFilter, IOrderedFilter
    {
        private readonly AspNetMvcTracingOptions _options;
        private readonly ModelStateInvalidFilter _modelStateInvalidFilter;

        public AspNetMvcRequestTracingFilter(
            IOptions<AspNetMvcTracingOptions> options,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions,
            ILoggerFactory loggerFactory)
        {
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(apiBehaviorOptions, nameof(apiBehaviorOptions));
            Guard.NotNull(loggerFactory, nameof(loggerFactory));

            _options = options.Value;
            _modelStateInvalidFilter = new ModelStateInvalidFilter(
                apiBehaviorOptions.Value,
                loggerFactory.CreateLogger(typeof(ModelStateInvalidFilter)));
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

            if (_options.InitialSuppressModelStateInvalidFilter == false)
            {
                _modelStateInvalidFilter.OnActionExecuting(context);
            }
            
            if (context.Result is null)
            {
                await next();
            }
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

        public int Order => _modelStateInvalidFilter.Order - 1;
    }
}