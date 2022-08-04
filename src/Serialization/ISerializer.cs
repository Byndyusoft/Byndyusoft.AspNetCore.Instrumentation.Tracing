using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization
{
    public interface ISerializer
    {
        ValueTask<string?> SerializeAsync(object? value, AspNetMvcTracingOptions options,
            CancellationToken cancellationToken);
    }
}