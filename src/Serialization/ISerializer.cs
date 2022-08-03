using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization
{
    public interface ISerializer
    {
        ValueTask<string?> SerializeRequestParamAsync(object? value, AspNetMvcRequestTracingOptions options,
            CancellationToken cancellationToken);

        ValueTask<string?> SerializeResponseBodyAsync(object? value, AspNetMvcResponseTracingOptions options,
            CancellationToken cancellationToken);
    }
}