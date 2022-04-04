using System;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal
{
    internal static class Guard
    {
        public static T NotNull<T>(T value, string paramName)
        {
            if (value is null)
                throw new ArgumentNullException(paramName);
            return value;
        }

        public static int? NotNegative(int? value, string paramName)
        {
            return value switch
            {
                null => value,
                <= 0 => throw new ArgumentOutOfRangeException(paramName),
                _ => value
            };
        }
    }
}