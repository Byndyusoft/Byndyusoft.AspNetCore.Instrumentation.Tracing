using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Example.Services;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Example
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddTracing(options =>
                {
                    options.ValueMaxStringLength = 50;
                    options.Formatter = new SystemTextJsonFormatter
                    {
                        Options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
                        {
                            Converters =
                            {
                                new JsonStringEnumConverter()
                            }
                        }
                    };
                });

            services.AddSingleton<IService, Service>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "My API", Version = "v1"});

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
            });

            services.AddOpenTelemetryTracing(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(Configuration.GetValue<string>("Jaeger:ServiceName")))
                    .AddAspNetCoreInstrumentation(
                        options =>
                        {
                            options.Filter = context => context.Request.Path.StartsWithSegments("/swagger") == false;
                        })
                    .AddConsoleExporter()
                    .AddJaegerExporter(Configuration.GetSection("Jaeger").Bind);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();
            app.UseSwagger();
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"); });
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}