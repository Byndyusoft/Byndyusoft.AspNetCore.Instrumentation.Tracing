using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization
{
    public abstract class SerializerBase : ISerializer
    {
        public async ValueTask<string?> SerializeRequestParamAsync(object? value, AspNetMvcRequestTracingOptions options,
            CancellationToken cancellationToken = default)
        {
            if (value is null)
                return "null";

            using var stream = new StringLimitStream(options.ValueMaxStringLength);

            await SerializeValueAsync(value, stream, options, cancellationToken)
                .ConfigureAwait(false);

            return stream.GetString();
        }
        
        public async ValueTask<string?> SerializeResponseBodyAsync(object? value, AspNetMvcResponseTracingOptions options,
            CancellationToken cancellationToken = default)
        {
            if (value is null)
                return null;

            using var stream = new StringLimitStream(options.ValueMaxStringLength);

            await SerializeValueAsync(value, stream, options, cancellationToken)
                .ConfigureAwait(false);

            return stream.GetString();
        }

        protected abstract ValueTask SerializeValueAsync(object value, Stream stream, AspNetMvcTracingOptions options,
            CancellationToken cancellationToken);
    }
}