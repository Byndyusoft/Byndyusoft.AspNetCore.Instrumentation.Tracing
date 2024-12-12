using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization
{
    public abstract class FormatterBase : IFormatter
    {
        public async ValueTask<string?> FormatAsync(
            object? value,
            int? valueMaxStringLength,
            CancellationToken cancellationToken = default)
        {
            if (value is null)
                return "null";

            using var stream = new StringLimitStream(valueMaxStringLength);

            await FormatValueAsync(value, stream, cancellationToken)
                .ConfigureAwait(false);

            return stream.GetString();
        }

        protected abstract ValueTask FormatValueAsync(
            object value,
            Stream stream,
            CancellationToken cancellationToken);
    }
}