using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Byndyusoft.MaskedSerialization.Newtonsoft.Extensions;
using Newtonsoft.Json;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Serialization.Json
{
    public class NewtonsoftJsonFormatter : FormatterBase
    {
        private JsonSerializerSettings _settings;

        public NewtonsoftJsonFormatter()
        {
            _settings = new JsonSerializerSettings();
            _settings.SetupSettingsForMaskedSerialization();
        }

        public JsonSerializerSettings Settings
        {
            get => _settings;
            set => _settings = Guard.NotNull(value, nameof(Settings));
        }

        protected override async ValueTask FormatValueAsync(
            object value,
            Stream stream,
            AspNetMvcTracingOptions options,
            CancellationToken cancellationToken)
        {
            var writer = new StreamWriter(stream, null!, -1, true);
            using var jsonWriter = new JsonTextWriter(writer);

            var serializer = JsonSerializer.Create(Settings);
            serializer.Serialize(jsonWriter, value);

            await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}