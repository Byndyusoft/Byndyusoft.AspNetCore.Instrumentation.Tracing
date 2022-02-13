namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public class AspNetMvcRequestTracingOptions : AspNetMvcTracingOptions
    {
        internal void Configure(AspNetMvcTracingOptions options)
        {
            JsonSerializerOptions = options.JsonSerializerOptions;
        }
    }
}