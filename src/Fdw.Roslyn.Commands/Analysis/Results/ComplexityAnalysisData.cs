using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Data returned by complexity analysis.
/// </summary>
public sealed class ComplexityAnalysisData
{
    /// <summary>
    /// Gets or sets the list of analyzed methods.
    /// </summary>
    public required IReadOnlyList<MethodComplexity> Methods { get; init; }

    /// <summary>
    /// Gets or sets the list of methods exceeding the threshold.
    /// </summary>
    public required IReadOnlyList<MethodComplexity> HighComplexityMethods { get; init; }

    /// <summary>
    /// Gets or sets the threshold used.
    /// </summary>
    public required int Threshold { get; init; }

    /// <summary>
    /// Gets or sets the total count of methods.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Gets or sets the count of high complexity methods.
    /// </summary>
    public required int HighCount { get; init; }
}