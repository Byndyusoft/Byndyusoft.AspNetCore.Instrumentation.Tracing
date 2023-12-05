using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization.Json;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public class AspNetMvcTracingOptions
    {
        public const int DefaultValueMaxStringLength = 2000;

        private IFormatter _formatter;
        private int? _valueMaxStringLength;

        public AspNetMvcTracingOptions()
        {
            _formatter = new NewtonsoftJsonFormatter();
            _valueMaxStringLength = DefaultValueMaxStringLength;
        }

        public IFormatter Formatter
        {
            get => _formatter;
            set => _formatter = Guard.NotNull(value, nameof(Formatter));
        }
        
        public int? ValueMaxStringLength
        {
            get => _valueMaxStringLength;
            set => _valueMaxStringLength = Guard.NotNegative(value, nameof(ValueMaxStringLength));
        }

        public bool LogRequestInTraces { get; set; } = true;

        public bool TagRequestParamsInTraces { get; set; } = false;

        public bool LogResponseInTraces { get; set; } = true;

        internal ValueTask<string?> FormatAsync(object? value, CancellationToken cancellationToken = default)
        {
            return Formatter.FormatAsync(value, this, cancellationToken);
        }

        internal void Configure(AspNetMvcTracingOptions options)
        {
            Formatter = options.Formatter;
            ValueMaxStringLength = options.ValueMaxStringLength;
            LogRequestInTraces = options.LogRequestInTraces;
            TagRequestParamsInTraces = options.TagRequestParamsInTraces;
            LogResponseInTraces = options.LogResponseInTraces;
        }
    }
}