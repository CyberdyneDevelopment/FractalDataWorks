using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.Tests.Logging;

/// <summary>
/// An <see cref="ILogger"/> that keeps every entry it is handed, already formatted.
/// </summary>
/// <remarks>
/// Why a recorder rather than a Moq of <see cref="ILogger"/>: what needs pinning is the EventId, the
/// level and the FORMATTED text a generated MessageLogging method emits, and a mock can only assert
/// against the opaque state object before the formatter has run — which is the half that says
/// nothing about what an operator will actually read.
///
/// Every level reports enabled because the generated methods guard on <see cref="IsEnabled"/>. A
/// recorder that disabled Trace would record nothing for the Trace methods while they still returned
/// their message, so the test would pass having observed no log line at all.
/// </remarks>
internal sealed class RecordingLogger : ILogger
{
    private readonly List<(LogLevel Level, EventId EventId, string Message)> _entries = [];

    /// <summary>Gets what was logged, in order.</summary>
    public IReadOnlyList<(LogLevel Level, EventId EventId, string Message)> Entries => _entries;

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

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
        ArgumentNullException.ThrowIfNull(formatter);

        _entries.Add((logLevel, eventId, formatter(state, exception)));
    }
}
