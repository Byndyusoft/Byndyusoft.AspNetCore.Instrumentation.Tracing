namespace Byndyusoft.AspNetCore.Instrumentation.Tracing;

internal static class HttpResponseKeys
{
    public static class Headers
    {
        public const string ContentType = "http.response.header.content_type";
        public const string ContentLength = "http.response.header.content_length";
    }

    public const string Body = "http.response.body";
}