// ReSharper disable CheckNamespace

using System;
using Byndyusoft.AspNetCore.Instrumentation.Tracing;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Extension methods for adding MessagePack formatters to MVC.
    /// </summary>
    public static class TracingMvcBuilderExtensions
    {
        /// <returns>The <see cref="IMvcBuilder" />.</returns>
        public static IMvcBuilder AddTracing(this IMvcBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder));

            return builder
                .AddRequestTracing()
                .AddResponseTracing();
        }

        public static IMvcBuilder AddTracing(this IMvcBuilder builder, Action<AspNetMvcTracingOptions> configure)
        {
            Guard.NotNull(builder, nameof(builder));
            Guard.NotNull(configure, nameof(configure));

            var options = new AspNetMvcTracingOptions();
            configure(options);

            return builder
                .AddRequestTracing(c => c.Configure(options))
                .AddResponseTracing(c => c.Configure(options));
        }

        /// <returns>The <see cref="IMvcBuilder" />.</returns>
        public static IMvcBuilder AddRequestTracing(this IMvcBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder));

            return AddRequestTracing(builder, Actions.Empty<AspNetMvcRequestTracingOptions>());
        }

        /// <returns>The <see cref="IMvcBuilder" />.</returns>
        public static IMvcBuilder AddRequestTracing(this IMvcBuilder builder,
            Action<AspNetMvcRequestTracingOptions> configure)
        {
            Guard.NotNull(builder, nameof(builder));
            Guard.NotNull(configure, nameof(configure));

            builder.Services.Configure(configure);

            AddCore(builder);

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, RequestTracingMvcOptionsSetup>());
            return builder;
        }

        /// <returns>The <see cref="IMvcBuilder" />.</returns>
        public static IMvcBuilder AddResponseTracing(this IMvcBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder));

            return AddResponseTracing(builder, Actions.Empty<AspNetMvcResponseTracingOptions>());
        }

        public static IMvcBuilder AddResponseTracing(this IMvcBuilder builder,
            Action<AspNetMvcResponseTracingOptions> configure)
        {
            Guard.NotNull(builder, nameof(builder));
            Guard.NotNull(configure, nameof(configure));

            builder.Services.Configure(configure);

            AddCore(builder);

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, ResponseTracingMvcOptionsSetup>());
            return builder;
        }

        private static void AddCore(IMvcBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder));

            builder.Services.TryAddTransient<ISerializer, Serializer>();
        }
    }
}