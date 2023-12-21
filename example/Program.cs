using Byndyusoft.Logging.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Configuration;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Example
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices(services => services.AddStaticTelemetryItemCollector()
                    .WithBuildConfiguration()
                    .WithAspNetCoreEnvironment()
                    .WithServiceName("Test Service")
                    .WithApplicationVersion("1.0.0"))
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseSerilog((context, configuration) => configuration
                        .UseConsoleWriterSettings()
                        .OverrideDefaultLoggers()
                        .ReadFrom.Configuration(context.Configuration)
                        .UseOpenTelemetryTraces()
                        .WriteToOpenTelemetry()
                        .Enrich.WithPropertyDataAccessor()
                        .Enrich.WithStaticTelemetryItems());
                });
        }
    }
}