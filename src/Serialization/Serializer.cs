using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization
{
    internal class Serializer : ISerializer
    {
        public async Task<string?> SerializeRequestParamAsync(object? value, AspNetMvcRequestTracingOptions options,
            CancellationToken cancellationToken = default)
        {
            var result = await SerializeValueAsync(value, options, cancellationToken)
                .ConfigureAwait(false);
            return result ?? "null";
        }

        public Task<string?> SerializeResponseBodyAsync(object? value, AspNetMvcResponseTracingOptions options,
            CancellationToken cancellationToken = default)
        {
            return SerializeValueAsync(value, options, cancellationToken);
        }

        private static async Task<string?> SerializeValueAsync(object? value, AspNetMvcTracingOptions options,
            CancellationToken cancellationToken)
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