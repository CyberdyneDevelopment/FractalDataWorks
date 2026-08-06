using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Data returned by nullability analysis.
/// </summary>
public sealed class NullabilityAnalysisData
{
    /// <summary>
    /// Gets or sets the list of analyzed symbols.
    /// </summary>
    public required IReadOnlyList<NullabilitySymbol> Symbols { get; init; }

    /// <summary>
    /// Gets or sets the summary statistics.
    /// </summary>
    public required NullabilitySummary Summary { get; init; }
}