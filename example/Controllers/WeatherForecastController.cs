using System;
using System.IO;
using System.Linq;
using System.Threading;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Example.Models;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Example.Services;
using Byndyusoft.Telemetry.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Example.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : Controller
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IService _service;

        public WeatherForecastController(
            ILogger<WeatherForecastController> logger,
            IService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet("{id}")]
        public IActionResult Get(
            [FromServices] IService service,
            [FromRoute] int id,
            [FromQuery] string name,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var forecast = service.GetWeatherForecasts().ToArray();

            _logger.LogInformation("Weather Got");

            return Ok(forecast);
        }

        [HttpPost]
        public IActionResult Post(WeatherForecast model, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Ok(model);
        }

        [HttpPost("file")]
        public IActionResult SendFile(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            var base64 = Convert.ToBase64String(memoryStream.ToArray());
            return Ok(base64);
        }

        [HttpPost("stream")]
        public IActionResult ReturnStream(IFormFile file)
        {
            var stream = file.OpenReadStream();

            return File(stream, file.ContentType);
        }
    }
}