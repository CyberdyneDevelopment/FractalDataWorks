using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Data returned by exception usage analysis.
/// </summary>
public sealed class ExceptionUsageAnalysisData
{
    /// <summary>
    /// Gets or sets the total count of throw statements.
    /// </summary>
    public required int ThrowCount { get; init; }

    /// <summary>
    /// Gets or sets the total count of try-catch blocks.
    /// </summary>
    public required int TryCatchCount { get; init; }

    /// <summary>
    /// Gets or sets the count of Result pattern candidates.
    /// </summary>
    public required int ResultPatternCandidates { get; init; }

    /// <summary>
    /// Gets or sets the project filter applied.
    /// </summary>
    public required string ProjectFilter { get; init; }

    /// <summary>
    /// Gets or sets the list of throw statements.
    /// </summary>
    public required IReadOnlyList<ThrowStatementInfo> ThrowStatements { get; init; }

    /// <summary>
    /// Gets or sets the list of try-catch blocks.
    /// </summary>
    public required IReadOnlyList<TryCatchBlockInfo> TryCatchBlocks { get; init; }
}