using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of a log viewer page model.
/// </summary>
public sealed class LogViewerPageModel : ILogViewerPageModel
{
    private readonly List<LogEntry> _entries = [];
    private readonly List<string> _availableSources = [];

    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string Title { get; set; } = "Logs";

    /// <inheritdoc />
    public IReadOnlyList<ILogEntry> Entries => _entries;

    /// <inheritdoc />
    public ILogLevel MinimumLevel { get; set; } = LogLevels.Information;

    /// <inheritdoc />
    public string? SearchText { get; set; }

    /// <inheritdoc />
    public string? SourceFilter { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<string> AvailableSources => _availableSources;

    /// <inheritdoc />
    public DateTime? StartTime { get; set; }

    /// <inheritdoc />
    public DateTime? EndTime { get; set; }

    /// <inheritdoc />
    public bool IsStreaming { get; set; }

    /// <inheritdoc />
    public bool AutoScroll { get; set; } = true;

    /// <inheritdoc />
    public int MaxEntries { get; set; } = 1000;

    /// <inheritdoc />
    public IPaginationState? Pagination { get; set; }

    /// <summary>
    /// Adds a log entry.
    /// </summary>
    public void AddEntry(LogEntry entry)
    {
        _entries.Add(entry);

        // Trim if over max
        while (_entries.Count > MaxEntries)
        {
            _entries.RemoveAt(0);
        }

        // Track sources
        if (!string.IsNullOrEmpty(entry.Source) && !_availableSources.Contains(entry.Source, StringComparer.Ordinal))
        {
            _availableSources.Add(entry.Source);
        }
    }

    /// <summary>
    /// Clears all entries.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
        _availableSources.Clear();
    }

    /// <summary>
    /// Gets filtered entries based on current filter settings.
    /// </summary>
    public IEnumerable<LogEntry> GetFilteredEntries()
    {
        var query = _entries.AsEnumerable();

        // Filter by level
        query = query.Where(e => e.Level.Id >= MinimumLevel.Id);

        // Filter by source
        if (!string.IsNullOrEmpty(SourceFilter))
        {
            query = query.Where(e => string.Equals(e.Source, SourceFilter, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by time range
        if (StartTime.HasValue)
        {
            query = query.Where(e => e.Timestamp >= StartTime.Value);
        }
        if (EndTime.HasValue)
        {
            query = query.Where(e => e.Timestamp <= EndTime.Value);
        }

        // Filter by search text
        if (!string.IsNullOrEmpty(SearchText))
        {
            query = query.Where(e =>
                e.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (e.Exception?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return query;
    }
}