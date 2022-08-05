using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Newtonsoft.Json;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization.Json
{
    public class NewtonsoftJsonSerializer : SerializerBase
    {
        private JsonSerializerSettings _settings = new();

        public JsonSerializerSettings Settings
        {
            get => _settings;
            set => _settings = Guard.NotNull(value, nameof(Settings));
        }

        protected override async ValueTask SerializeValueAsync(
            object value,
            Stream stream,
            AspNetMvcTracingOptions options,
            CancellationToken cancellationToken)
        {
            using var writer = new StreamWriter(stream);
            using var jsonWriter = new JsonTextWriter(writer);

            var serializer = JsonSerializer.Create(Settings);
            serializer.Serialize(jsonWriter, value);

            await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}