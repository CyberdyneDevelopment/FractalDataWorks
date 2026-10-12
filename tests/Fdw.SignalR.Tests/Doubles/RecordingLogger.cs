using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Fdw.SignalR.Tests.Doubles;

/// <summary>
/// An <see cref="ILogger{T}"/> that records every logged entry so tests can assert on level/EventId.
/// </summary>
/// <typeparam name="T">The category type.</typeparam>
public sealed class RecordingLogger<T> : ILogger<T>
{
    /// <summary>Gets the recorded log entries in order.</summary>
    public List<(LogLevel Level, int EventId, string Message)> Entries { get; } = new();

    /// <inheritdoc/>
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => NullScope.Instance;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, eventId.Id, formatter(state, exception)));
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
