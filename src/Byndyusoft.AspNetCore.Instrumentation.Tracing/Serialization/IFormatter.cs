using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization
{
    public interface IFormatter
    {
        ValueTask<string?> FormatAsync(object? value, AspNetMvcTracingOptions options,
            CancellationToken cancellationToken);
    }
}