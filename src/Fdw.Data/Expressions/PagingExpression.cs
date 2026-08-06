using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Implementation of IPagingExpression for SKIP/TAKE representation.
/// </summary>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class PagingExpression : IPagingExpression
{
    /// <summary>
    /// Gets or sets the number of records to skip.
    /// </summary>
    public required int Skip { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of records to return.
    /// Null means no limit.
    /// </summary>
    public int? Take { get; init; }
}
