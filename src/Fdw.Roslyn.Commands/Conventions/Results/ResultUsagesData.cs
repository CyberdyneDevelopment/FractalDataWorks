using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Data returned by Result usages analysis.
/// </summary>
public sealed class ResultUsagesData
{
    /// <summary>
    /// Gets or sets the total count of usages.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Gets or sets the project filter applied.
    /// </summary>
    public required string ProjectFilter { get; init; }

    /// <summary>
    /// Gets or sets the list of usages.
    /// </summary>
    public required IReadOnlyList<ResultUsageInfo> Usages { get; init; }
}