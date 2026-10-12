using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Data returned by message logging analysis.
/// </summary>
public sealed class MessageLoggingData
{
    /// <summary>
    /// Gets or sets the total count of logging methods.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Gets or sets the project filter applied.
    /// </summary>
    public required string ProjectFilter { get; init; }

    /// <summary>
    /// Gets or sets the event ID ranges per project.
    /// </summary>
    public required IReadOnlyDictionary<string, EventIdRange> EventIdRanges { get; init; }

    /// <summary>
    /// Gets or sets the list of logging methods.
    /// </summary>
    public required IReadOnlyList<LoggingMethodInfo> Methods { get; init; }
}