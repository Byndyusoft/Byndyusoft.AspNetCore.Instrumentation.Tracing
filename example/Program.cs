using Byndyusoft.Logging.Builders;
using Byndyusoft.Logging.Configuration;
using Microsoft.AspNetCore.Hosting;
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
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseSerilog((context, configuration) => configuration
                        .UseDefaultSettings(context.Configuration)
                        .UseOpenTelemetryTraces()
                        .WriteToOpenTelemetry(activityEventBuilder: StructuredActivityEventBuilder.Instance)
                        .Enrich.WithPropertyDataAccessor()
                        .Enrich.WithStaticTelemetryItems());
                });
        }
    }
}