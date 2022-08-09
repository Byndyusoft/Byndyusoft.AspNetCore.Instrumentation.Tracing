using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization.Json
{
    public class SystemTextJsonFormatter : FormatterBase
    {
        private JsonSerializerOptions _options = new();

        public JsonSerializerOptions Options
        {
            get => _options;
            set => _options = Guard.NotNull(value, nameof(Options));
        }

        protected override async ValueTask FormatValueAsync(
            object value, 
            Stream stream, 
            AspNetMvcTracingOptions options, 
            CancellationToken cancellationToken)
        {
            await JsonSerializer.SerializeAsync(stream, value, Options, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}