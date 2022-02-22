using Microsoft.AspNetCore.Mvc;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Services
{
    public class ActionResultBodyExtractor
    {
        public bool TryExtractBody(IActionResult? actionResult, out object? body)
        {
            var (value, result) = actionResult switch
            {
                FileStreamResult _ => ("stream", true),
                FileContentResult _ => ("byte[]", true),
                ObjectResult objectResult => (objectResult.Value, true),
                JsonResult jsonResult => (jsonResult.Value, true),
                ContentResult contentResult => (contentResult.Content, true),
                ViewResult viewResult => (new {viewResult.Model, viewResult.ViewData, viewResult.TempData}, true),
                _ => (null, false)
            };

            body = value;
            return result;
        }
    }
}