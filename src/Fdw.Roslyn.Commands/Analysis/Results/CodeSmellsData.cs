using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Data returned by code smells detection.
/// </summary>
public sealed class CodeSmellsData
{
    /// <summary>
    /// Gets or sets the list of detected smells.
    /// </summary>
    public required IReadOnlyList<CodeSmell> Smells { get; init; }

    /// <summary>
    /// Gets or sets the summary statistics.
    /// </summary>
    public required CodeSmellsSummary Summary { get; init; }
}