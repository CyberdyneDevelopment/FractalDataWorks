using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Tests.TestDoubles;

/// <summary>
/// An <see cref="ILoggerProvider"/> that keeps every message it is given.
/// </summary>
/// <remarks>
/// Asserting that a logger "is not NullLogger" only proves something was assigned. The question that
/// actually matters is whether a line emitted from inside a running translator reaches a sink, which
/// nothing short of capturing the output can answer.
/// </remarks>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _messages = new();

    /// <summary>Gets every message logged through this provider, in order.</summary>
    public IReadOnlyCollection<string> Messages => _messages;

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => new CapturingLogger(_messages);

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    private sealed class CapturingLogger : ILogger
    {
        private readonly ConcurrentQueue<string> _sink;

        public CapturingLogger(ConcurrentQueue<string> sink) => _sink = sink;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (formatter is null) throw new ArgumentNullException(nameof(formatter));

            _sink.Enqueue($"{logLevel}|{eventId.Id}|{formatter(state, exception)}");
        }
    }
}
