using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>A logger that keeps what is written to it, for a test that asserts a failure was logged where it was made.</summary>
internal sealed class CapturingLogger : ILogger
{
    /// <summary>Gets what was logged, in order: the level and the formatted message.</summary>
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception)));
}
