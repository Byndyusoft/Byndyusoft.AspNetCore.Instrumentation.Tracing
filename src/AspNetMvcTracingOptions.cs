using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization.Json;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public class AspNetMvcTracingOptions
    {
        public const int DefaultValueMaxStringLength = 2000;

        private ISerializer _serializer;
        private int? _valueMaxStringLength;

        public AspNetMvcTracingOptions()
        {
            _serializer = new NewtonsoftJsonSerializer();
            _valueMaxStringLength = DefaultValueMaxStringLength;
        }

        public ISerializer Serializer
        {
            get => _serializer;
            set => _serializer = Guard.NotNull(value, nameof(Serializer));
        }
        
        public int? ValueMaxStringLength
        {
            get => _valueMaxStringLength;
            set => _valueMaxStringLength = Guard.NotNegative(value, nameof(ValueMaxStringLength));
        }
    }
}