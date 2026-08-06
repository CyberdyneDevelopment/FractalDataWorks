using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Data returned by diagnostics retrieval.
/// </summary>
public sealed class DiagnosticsData
{
    /// <summary>
    /// Gets or sets the list of diagnostics.
    /// </summary>
    public required IReadOnlyList<DiagnosticInfo> Diagnostics { get; init; }

    /// <summary>
    /// Gets or sets the summary by severity.
    /// </summary>
    public required IReadOnlyDictionary<string, int> Summary { get; init; }

    /// <summary>
    /// Gets or sets the total count of diagnostics.
    /// </summary>
    public required int Count { get; init; }
}