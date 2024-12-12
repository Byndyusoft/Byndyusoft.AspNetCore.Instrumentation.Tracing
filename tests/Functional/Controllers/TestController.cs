using Microsoft.AspNetCore.Mvc;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Functional.Controllers
{
    [Controller]
    [Route("test")]
    public class TestController : ControllerBase
    {
        [HttpPost("echo")]
        public object Echo([FromBody] object model)
        {
            return model;
        }

        [HttpPost("stream")]
        public IActionResult ReturnStream()
        {
            return File(Request.Body, "application/octet-stream");
        }
    }
}