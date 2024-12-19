using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization.Json;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public class AspNetMvcTracingOptions
    {
        private IFormatter _formatter;
        private int? _valueMaxStringLength;

        public AspNetMvcTracingOptions()
        {
            _formatter = new SystemTextJsonFormatter();
            _valueMaxStringLength = null;

            EnrichTraceWithTaggedRequestParams = true;
            EnrichLogsWithParams = true;
            EnrichLogsWithHttpInfo = true;
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

        public bool EnrichTraceWithTaggedRequestParams { get; set; }

        public bool EnrichLogsWithParams { get; set; }

        public bool EnrichLogsWithHttpInfo { get; set; }

        internal ValueTask<string?> FormatAsync(object? value, CancellationToken cancellationToken = default)
        {
            return Formatter.FormatAsync(value, ValueMaxStringLength, cancellationToken);
        }

        internal void Configure(AspNetMvcTracingOptions options)
        {
            Formatter = options.Formatter;
            ValueMaxStringLength = options.ValueMaxStringLength;
            EnrichTraceWithTaggedRequestParams = options.EnrichTraceWithTaggedRequestParams;
            EnrichLogsWithParams = options.EnrichLogsWithParams;
            EnrichLogsWithHttpInfo = options.EnrichLogsWithHttpInfo;
        }
    }
}