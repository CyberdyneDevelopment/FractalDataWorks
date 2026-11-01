using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Data returned by Result handling validation.
/// </summary>
public sealed class ResultHandlingValidationData
{
    /// <summary>
    /// Gets or sets the total count of issues.
    /// </summary>
    public required int IssueCount { get; init; }

    /// <summary>
    /// Gets or sets the project filter applied.
    /// </summary>
    public required string ProjectFilter { get; init; }

    /// <summary>
    /// Gets or sets the list of issues.
    /// </summary>
    public required IReadOnlyList<ResultHandlingIssue> Issues { get; init; }
}