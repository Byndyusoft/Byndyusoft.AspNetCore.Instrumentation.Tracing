using System.Text.Json;
using System.Text.Json.Serialization;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public class AspNetMvcTracingOptions
    {
        private JsonSerializerOptions _jsonSerializerOptions = new();

        public AspNetMvcTracingOptions()
        {
            JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public JsonSerializerOptions JsonSerializerOptions
        {
            get => _jsonSerializerOptions;
            set => _jsonSerializerOptions = Guard.NotNull(value, nameof(value));
        }
    }
}