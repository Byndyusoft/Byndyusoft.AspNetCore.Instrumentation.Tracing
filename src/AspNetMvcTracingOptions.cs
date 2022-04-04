using System.Text.Json;
using System.Text.Json.Serialization;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public class AspNetMvcTracingOptions
    {
        public const int DefaultValueMaxStringLength = 2000;

        private JsonSerializerOptions _jsonSerializerOptions = new();
        private int? _valueMaxStringLength;

        public AspNetMvcTracingOptions()
        {
            JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            ValueMaxStringLength = DefaultValueMaxStringLength;
        }

        public JsonSerializerOptions JsonSerializerOptions
        {
            get => _jsonSerializerOptions;
            set => _jsonSerializerOptions = Guard.NotNull(value, nameof(JsonSerializerOptions));
        }

        public int? ValueMaxStringLength
        {
            get => _valueMaxStringLength;
            set => _valueMaxStringLength = Guard.NotNegative(value, nameof(ValueMaxStringLength));
        }
    }
}