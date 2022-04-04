namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public class AspNetMvcResponseTracingOptions : AspNetMvcTracingOptions
    {
        internal void Configure(AspNetMvcTracingOptions options)
        {
            JsonSerializerOptions = options.JsonSerializerOptions;
            ValueMaxStringLength = options.ValueMaxStringLength;
        }
    }
}