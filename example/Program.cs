using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Byndyusoft.Logging.Configuration;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Example.Services;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization.Json;
using Byndyusoft.Logging.Builders;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .UseDefaultSettings(context.Configuration)
    .UseOpenTelemetryTraces()
    .WriteToOpenTelemetry(activityEventBuilder: StructuredActivityEventBuilder.Instance)
    .Enrich.WithPropertyDataAccessor()
    .Enrich.WithStaticTelemetryItems()
);

var serviceName = builder
    .Configuration
    .GetValue<string>("Jaeger:ServiceName");

var services = builder.Services;
services
    .AddControllers()
    .AddTracing(
        options =>
        {
            options.EnrichTraceWithTaggedRequestParams = true;
            options.EnrichLogsWithParams = true;
            options.EnrichLogsWithHttpInfo = true;
            options.Formatter = new SystemTextJsonFormatter
            {
                Options = new JsonSerializerOptions
                {
                    Converters =
                    {
                        new JsonStringEnumConverter()
                    }
                }
            };
        }
    );
services.AddSingleton<IService, Service>();
services.AddSwaggerGen(
    c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "My API",
                Version = "v1"
            }
        );
    
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
    }
);
services
    .AddOpenTelemetry()
    .ConfigureResource(
        resource => resource.AddService(serviceName!)
    )
    .WithTracing(
        tracerBuilder =>
        {
            tracerBuilder
                .SetResourceBuilder(ResourceBuilder
                    .CreateDefault()
                    .AddStaticTelemetryItems())
                .AddAspNetCoreInstrumentation(
                    options =>
                    {
                        options.Filter = context =>
                            context
                                .Request
                                .Path
                                .StartsWithSegments("/swagger") == false;
                    }
                )
                .AddJaegerExporter(
                    builder
                        .Configuration
                        .GetSection("Jaeger")
                        .Bind
                )
                .AddConsoleExporter();
        }
    );
services.AddMvc();
services.ConfigureStaticTelemetryItemCollector()
    .WithBuildConfiguration()
    .WithAspNetCoreEnvironment()
    .WithServiceName("Test Service")
    .WithApplicationVersion("1.0.0");

var app = builder.Build();
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI(
    c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    }
);
app.UseRouting();
app.MapControllers();
app.Run();