using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public interface ISerializer
    {
        Task<string?> SerializeRequestParamAsync(object? value, AspNetMvcRequestTracingOptions options, CancellationToken cancellationToken);
        Task<string?> SerializeResponseBodyAsync(object? value, AspNetMvcResponseTracingOptions options, CancellationToken cancellationToken);
    }
}