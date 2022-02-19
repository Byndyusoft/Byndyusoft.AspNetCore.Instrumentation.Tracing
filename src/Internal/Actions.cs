using System;
using System.Diagnostics.CodeAnalysis;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal
{
    [ExcludeFromCodeCoverage]
    internal static class Actions
    {
        public static Action<T> Empty<T>()
        {
            return delegate { };
        }

        public static Action Empty()
        {
            return delegate { };
        }
    }
}