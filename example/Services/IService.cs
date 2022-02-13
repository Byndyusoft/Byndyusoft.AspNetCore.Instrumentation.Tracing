using System.Collections.Generic;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Example.Models;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Example.Services
{
    public interface IService
    {
        IEnumerable<WeatherForecast> GetWeatherForecasts();
    }
}