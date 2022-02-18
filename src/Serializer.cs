using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    internal class Serializer : ISerializer
    {
        public Task<string?> SerializeRequestParamAsync(object? value, AspNetMvcRequestTracingOptions options, CancellationToken cancellationToken)
        {
            return SerializeValueAsync(value, options, cancellationToken);
        }

        public Task<string?> SerializeResponseBodyAsync(object? value, AspNetMvcResponseTracingOptions options, CancellationToken cancellationToken)
        {
            return SerializeValueAsync(value, options, cancellationToken);
        }

        private async Task<string?> SerializeValueAsync(object? value, AspNetMvcTracingOptions options, CancellationToken cancellationToken)
        {
            if (value == null)
                return null;

            using var stream = new StringLimitStream(options.ValueMaxStringLength);

            await JsonSerializer.SerializeAsync(stream, value, options.JsonSerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            return stream.GetString();
        }
    }
}