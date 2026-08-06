using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Implementation of IJoinExpression for JOIN clause representation.
/// </summary>
/// <remarks>
/// Full join support will be implemented in future phases.
/// This provides basic structure for join operations.
/// </remarks>
public sealed class JoinExpression : IJoinExpression
{
    /// <summary>
    /// Gets or sets the target container name to join with.
    /// </summary>
    public required string TargetContainerName { get; init; }

    /// <summary>
    /// Gets or sets the join type (INNER, LEFT, RIGHT, FULL).
    /// </summary>
    public required string JoinType { get; init; }

    /// <summary>
    /// Gets or sets the join conditions.
    /// </summary>
    public required IReadOnlyList<(string LeftField, string RightField)> JoinConditions { get; init; }
}
