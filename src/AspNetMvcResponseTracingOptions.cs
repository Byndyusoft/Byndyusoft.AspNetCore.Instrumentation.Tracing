namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public class AspNetMvcResponseTracingOptions : AspNetMvcTracingOptions
    {
        internal void Configure(AspNetMvcTracingOptions options)
        {
            Serializer = options.Serializer;
            ValueMaxStringLength = options.ValueMaxStringLength;
        }
    }
}