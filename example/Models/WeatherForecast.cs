using System;
using System.Runtime.Serialization;
using Byndyusoft.Telemetry.Abstraction.Attributes;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Example.Models
{
    [DataContract]
    public class WeatherForecast
    {
        [DataMember] [TelemetryItem] public DateTime Date { get; set; }

        [DataMember] [TelemetryItem] public int TemperatureC { get; set; }

        [DataMember] public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        [DataMember] public string Summary { get; set; } = default!;
    }
}