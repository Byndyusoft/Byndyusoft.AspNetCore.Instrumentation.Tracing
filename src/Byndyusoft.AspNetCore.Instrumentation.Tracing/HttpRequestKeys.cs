namespace Byndyusoft.AspNetCore.Instrumentation.Tracing;

internal static class HttpRequestKeys
{
    public const string Url = "http.request.url";
    public const string ParamsPrefix = "http.request.params";

    public static class Headers
    {
        public const string Accept = "http.request.header.accept";
        public const string ContentType = "http.request.header.content_type";
        public const string ContentLength = "http.request.header.content_length";
    }
}