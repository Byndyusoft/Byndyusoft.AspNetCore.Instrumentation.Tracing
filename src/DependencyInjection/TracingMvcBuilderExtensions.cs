// ReSharper disable CheckNamespace

using System;
using Byndyusoft.AspNetCore.Instrumentation.Tracing;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.DependencyInjection;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization;
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
        public static IMvcBuilder AddTracing(this IMvcBuilder builder,
            Action<AspNetMvcTracingOptions>? configure = null)
        {
            Guard.NotNull(builder, nameof(builder));

            var options = new AspNetMvcTracingOptions();
            configure?.Invoke(options);

            return builder
                .AddRequestTracing(c => c.Configure(options))
                .AddResponseTracing(c => c.Configure(options));
        }

        /// <returns>The <see cref="IMvcCoreBuilder" />.</returns>
        public static IMvcCoreBuilder AddTracing(this IMvcCoreBuilder builder,
            Action<AspNetMvcTracingOptions>? configure = null)
        {
            Guard.NotNull(builder, nameof(builder));

            var options = new AspNetMvcTracingOptions();
            configure?.Invoke(options);

            return builder
                .AddRequestTracing(c => c.Configure(options))
                .AddResponseTracing(c => c.Configure(options));
        }

        /// <returns>The <see cref="IMvcBuilder" />.</returns>
        public static IMvcBuilder AddRequestTracing(this IMvcBuilder builder,
            Action<AspNetMvcRequestTracingOptions>? configure = null)
        {
            Guard.NotNull(builder, nameof(builder));

            if (configure != null) builder.Services.Configure(configure);

            AddCore(builder.Services);

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, RequestTracingMvcOptionsSetup>());
            return builder;
        }

        /// <returns>The <see cref="IMvcCoreBuilder" />.</returns>
        public static IMvcCoreBuilder AddRequestTracing(this IMvcCoreBuilder builder,
            Action<AspNetMvcRequestTracingOptions>? configure = null)
        {
            Guard.NotNull(builder, nameof(builder));

            if (configure != null) builder.Services.Configure(configure);

            AddCore(builder.Services);

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, RequestTracingMvcOptionsSetup>());
            return builder;
        }

        /// <returns>The <see cref="IMvcBuilder" />.</returns>
        public static IMvcBuilder AddResponseTracing(this IMvcBuilder builder,
            Action<AspNetMvcResponseTracingOptions>? configure = null)
        {
            Guard.NotNull(builder, nameof(builder));

            if (configure != null) builder.Services.Configure(configure);

            AddCore(builder.Services);

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, ResponseTracingMvcOptionsSetup>());
            return builder;
        }

        /// <returns>The <see cref="IMvcCoreBuilder" />.</returns>
        public static IMvcCoreBuilder AddResponseTracing(this IMvcCoreBuilder builder,
            Action<AspNetMvcResponseTracingOptions>? configure = null)
        {
            Guard.NotNull(builder, nameof(builder));

            if (configure != null) builder.Services.Configure(configure);

            AddCore(builder.Services);

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, ResponseTracingMvcOptionsSetup>());
            return builder;
        }

        private static void AddCore(IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));

            services.TryAddTransient<ISerializer, Serializer>();
        }
    }
}