using System;
using System.Diagnostics.CodeAnalysis;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    [ExcludeFromCodeCoverage]
    internal static class Actions
    {
        public static Action<T> Empty<T>() => delegate { };

        public static Action Empty() => delegate { };
    }
}