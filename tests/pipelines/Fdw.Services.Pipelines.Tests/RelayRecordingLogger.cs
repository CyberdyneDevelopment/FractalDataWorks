using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>A logger that keeps what it is given, so a test can say which coded messages a class wrote.</summary>
internal sealed class RelayRecordingLogger<T> : ILogger<T>
{
    /// <summary>Gets the event ids written, in order.</summary>
    public List<int> EventIds { get; } = [];

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => EventIds.Add(eventId.Id);
}
