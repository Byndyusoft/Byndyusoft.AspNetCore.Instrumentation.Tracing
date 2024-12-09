using System;
using System.Collections.Generic;
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
            var request = context.Request.Body;
            var buffSize = 4 * 1024;
            var buff = new byte[buffSize];
            var offset = 0;
            var obj = new List<byte>();
            while (await request.ReadAsync(buff, offset, buffSize) > 0)
            {
                obj.AddRange(buff);
                offset += buffSize;
            }
            _logger.LogInformation(
                "{TraceEventName} Parameters: MessageBody = {MessageBody}",
                "Consuming message",
                JsonConvert.SerializeObject(obj, _serializerSettings)
            );
            await next(context);
        }
    }
}