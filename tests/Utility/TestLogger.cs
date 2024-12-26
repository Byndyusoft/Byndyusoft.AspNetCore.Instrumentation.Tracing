using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Internal;
using Microsoft.Extensions.Logging;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Utility;

public class TestLogger(string name) : ILogger
{
    public List<string> Messages { get; } = new();
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Messages.Add(formatter(state, exception));
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        Messages.Add(state!.ToString()!);
        return default!;
    }
}

public class TestLoggerProvider : ILoggerProvider
{
    public readonly ConcurrentDictionary<string, TestLogger> Loggers =
        new(StringComparer.OrdinalIgnoreCase);
    public void Dispose()
    {
        Loggers.Clear();
    }

    public ILogger CreateLogger(string categoryName)=>
        Loggers.GetOrAdd(categoryName, name => new TestLogger(name));
}