using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Compilation.Results;

/// <summary>
/// Data returned by get diagnostics operation.
/// </summary>
public sealed class DiagnosticsData
{
    /// <summary>
    /// Gets or sets the file path (if scoped to a document).
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets or sets the project name (if scoped to a project).
    /// </summary>
    public string? ProjectName { get; init; }

    /// <summary>
    /// Gets or sets the diagnostic count.
    /// </summary>
    public required int DiagnosticCount { get; init; }

    /// <summary>
    /// Gets or sets the list of diagnostics.
    /// </summary>
    public required IReadOnlyList<DiagnosticInfo> Diagnostics { get; init; }

    /// <summary>
    /// Gets or sets the minimum severity filter applied.
    /// </summary>
    public required string MinSeverity { get; init; }
}
