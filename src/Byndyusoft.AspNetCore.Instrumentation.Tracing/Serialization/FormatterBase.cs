using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization
{
    public abstract class FormatterBase : IFormatter
    {
        public async ValueTask<string?> FormatAsync(object? value, AspNetMvcTracingOptions options,
            CancellationToken cancellationToken = default)
        {
            if (value is null)
                return "null";

            using var stream = new StringLimitStream(options.ValueMaxStringLength);

            await FormatValueAsync(value, stream, options, cancellationToken)
                .ConfigureAwait(false);

            return stream.GetString();
        }

        protected abstract ValueTask FormatValueAsync(object value, Stream stream, AspNetMvcTracingOptions options,
            CancellationToken cancellationToken);
    }
}