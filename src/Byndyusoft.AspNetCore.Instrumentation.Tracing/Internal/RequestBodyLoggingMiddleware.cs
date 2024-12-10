using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization;
using Byndyusoft.MaskedSerialization.Newtonsoft.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal
{
    public class ConsumedMessageLoggingMiddleware : IMiddleware
    {
        private readonly ILogger<ConsumedMessageLoggingMiddleware> _logger;
        private readonly JsonSerializerSettings _serializerSettings;

        public ConsumedMessageLoggingMiddleware(ILogger<ConsumedMessageLoggingMiddleware> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializerSettings = MaskedSerializationHelper
                .GetSettingsForMaskedSerialization()
                .ApplyDefaultSettings();
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Method == "GET")
            {
                await next(context);
                return;
            }

            var requestBody = context.Request.Body;
            using var memoryStream = new MemoryStream();
            await requestBody.CopyToAsync(memoryStream);
            var str = Encoding.UTF8.GetString(memoryStream.ToArray());
            var body = JsonConvert.SerializeObject(str, _serializerSettings);
            _logger.LogInformation(
                "{{\"MessageBody\":{MessageBody}}}",
                body
            );
            await next(context);
        }
    }
}