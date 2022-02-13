using System;
using System.IO;
using System.Linq;
using System.Threading;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Example.Models;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Example.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Example.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : Controller
    {
        private readonly IService _service;

        public WeatherForecastController(IService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult Get([FromServices] IService service, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var forecast = service.GetWeatherForecasts().ToArray();

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

        [HttpGet("mvc")]
        public IActionResult Mvc()
        {
            var forecast = _service.GetWeatherForecasts().ToArray();

            return View(forecast);
        }
    }
}