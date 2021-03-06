using System;
using System.Collections.Generic;
using System.Linq;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Example.Models;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Example.Services
{
    public class Service : IService
    {
        private static readonly string[] Summaries =
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public IEnumerable<WeatherForecast> GetWeatherForecasts()
        {
            var rng = new Random();
            var forecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = Summaries[rng.Next(Summaries.Length)]
                })
                .ToArray();
            return forecast;
        }
    }
}