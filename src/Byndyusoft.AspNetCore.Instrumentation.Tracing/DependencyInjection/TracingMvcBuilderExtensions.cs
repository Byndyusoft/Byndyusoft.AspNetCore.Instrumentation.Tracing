// ReSharper disable CheckNamespace

using System;
using Byndyusoft.AspNetCore.Instrumentation.Tracing;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Extension methods for adding Tracing to MVC.
    /// </summary>
    public static class TracingMvcBuilderExtensions
    {
        /// <returns>The <see cref="IMvcBuilder" />.</returns>
        public static IMvcBuilder AddTracing(this IMvcBuilder builder,
            Action<AspNetMvcTracingOptions>? configure = null)
        {
            Guard.NotNull(builder, nameof(builder));

            return builder
                .AddRequestTracing(configure)
                .AddResponseTracing(configure);
        }

        /// <returns>The <see cref="IMvcCoreBuilder" />.</returns>
        public static IMvcCoreBuilder AddTracing(this IMvcCoreBuilder builder,
            Action<AspNetMvcTracingOptions>? configure = null)
        {
            Guard.NotNull(builder, nameof(builder));

            return builder
                .AddRequestTracing(configure)
                .AddResponseTracing(configure);
        }

        /// <returns>The <see cref="IMvcBuilder" />.</returns>
        public static IMvcBuilder AddRequestTracing(this IMvcBuilder builder,
            Action<AspNetMvcTracingOptions>? configure = null)
        {
            Guard.NotNull(builder, nameof(builder));

            builder.Services.AddRequestTracingCore(configure);

            return builder;
        }

        /// <returns>The <see cref="IMvcCoreBuilder" />.</returns>
        public static IMvcCoreBuilder AddRequestTracing(this IMvcCoreBuilder builder,
            Action<AspNetMvcTracingOptions>? configure = null)
        {
            Guard.NotNull(builder, nameof(builder));

            builder.Services.AddRequestTracingCore(configure);

            return builder;
        }

        /// <returns>The <see cref="IMvcBuilder" />.</returns>
        public static IMvcBuilder AddResponseTracing(this IMvcBuilder builder,
            Action<AspNetMvcTracingOptions>? configure = null)
        {
            Guard.NotNull(builder, nameof(builder));

            builder.Services.AddResponseTracingCore(configure);

            return builder;
        }

        /// <returns>The <see cref="IMvcCoreBuilder" />.</returns>
        public static IMvcCoreBuilder AddResponseTracing(this IMvcCoreBuilder builder,
            Action<AspNetMvcTracingOptions>? configure = null)
        {
            Guard.NotNull(builder, nameof(builder));

            builder.Services.AddResponseTracingCore(configure);

            return builder;
        }

        private static void AddResponseTracingCore(this IServiceCollection services,
            Action<AspNetMvcTracingOptions>? configure)
        {
            if (configure != null)
            {
                services.Configure(configure);
            }

            services.PostConfigure<MvcOptions>(options =>
            {
                options.Filters.Add<AspNetMvcResponseTracingFilter>();
            });
        }

        private static void AddRequestTracingCore(this IServiceCollection services,
            Action<AspNetMvcTracingOptions>? configure)
        {
            if (configure != null)
            {
                services.Configure(configure);
            }

            services.AddSingleton(typeof(IPostConfigureOptions<MvcOptions>), typeof(PostConfigureMvcOptions));
        }

        internal sealed class PostConfigureMvcOptions(IServiceProvider serviceProvider) : IPostConfigureOptions<MvcOptions>
        {
            public void PostConfigure(string? name, MvcOptions options)
            {
                var tracingOptions = serviceProvider.GetRequiredService<IOptions<AspNetMvcTracingOptions>>().Value;
                var apiBehaviorOptions = serviceProvider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
                tracingOptions.InitialSuppressModelStateInvalidFilter = apiBehaviorOptions.SuppressModelStateInvalidFilter;
                apiBehaviorOptions.SuppressModelStateInvalidFilter = true;

                options.Filters.Add<AspNetMvcRequestTracingFilter>();
            }
        }
    }
}