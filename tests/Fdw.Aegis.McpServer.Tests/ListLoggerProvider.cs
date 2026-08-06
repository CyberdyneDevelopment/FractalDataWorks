using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Fdw.Aegis.McpServer.Tests;

/// <summary>
/// An <see cref="ILoggerProvider"/> that collects every rendered log line into a list, so the
/// non-exposure proof can assert the resolved secret never appears in ANY log line — not just the
/// tool responses.
/// </summary>
public sealed class ListLoggerProvider : ILoggerProvider
{
    private readonly List<string> _lines = [];
    private readonly object _gate = new();

    /// <summary>Gets a snapshot of every line logged so far.</summary>
    public IReadOnlyList<string> Lines
    {
        get
        {
            lock (_gate)
            {
                return _lines.ToArray();
            }
        }
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new CollectingLogger(this, categoryName);

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private void Add(string line)
    {
        lock (_gate)
        {
            _lines.Add(line);
        }
    }

    private sealed class CollectingLogger : ILogger
    {
        private readonly ListLoggerProvider _owner;
        private readonly string _category;

        public CollectingLogger(ListLoggerProvider owner, string category)
        {
            _owner = owner;
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _owner.Add($"[{logLevel}] {_category}: {formatter(state, exception)}");
        }
    }
}
