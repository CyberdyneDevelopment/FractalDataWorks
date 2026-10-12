using System;
using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Represents a log viewer page for viewing and filtering logs.
/// </summary>
/// <remarks>
/// Log viewer pages support:
/// - Real-time streaming of new log entries
/// - Level filtering (Error, Warning, Info, Debug, etc.)
/// - Text search
/// - Source/category filtering
/// - Time range selection
/// - Export capabilities
/// </remarks>
public interface ILogViewerPageModel
{
    /// <summary>
    /// Gets the unique identifier for this page.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the page title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the current log entries being displayed.
    /// </summary>
    IReadOnlyList<ILogEntry> Entries { get; }

    /// <summary>
    /// Gets or sets the minimum log level to display.
    /// </summary>
    ILogLevel MinimumLevel { get; set; }

    /// <summary>
    /// Gets or sets the search text filter.
    /// </summary>
    string? SearchText { get; set; }

    /// <summary>
    /// Gets or sets the source/category filter.
    /// </summary>
    string? SourceFilter { get; set; }

    /// <summary>
    /// Gets the available log sources for filtering.
    /// </summary>
    IReadOnlyList<string> AvailableSources { get; }

    /// <summary>
    /// Gets or sets the start time for the time range filter.
    /// </summary>
    DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time for the time range filter.
    /// </summary>
    DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets a value indicating whether live streaming is enabled.
    /// </summary>
    bool IsStreaming { get; }

    /// <summary>
    /// Gets a value indicating whether auto-scroll is enabled.
    /// </summary>
    bool AutoScroll { get; set; }

    /// <summary>
    /// Gets the maximum number of entries to keep in memory.
    /// </summary>
    int MaxEntries { get; }

    /// <summary>
    /// Gets the pagination state for historical log viewing.
    /// </summary>
    IPaginationState? Pagination { get; }
}