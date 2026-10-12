using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Implementation of IJoinDefinition for compound query joins.
/// </summary>
public sealed class JoinDefinition : IJoinDefinition
{
    /// <summary>
    /// Gets or inits the container name to join with.
    /// </summary>
    public required string ContainerName { get; init; }

    /// <summary>
    /// Gets or inits the alias for this joined container.
    /// </summary>
    public string? Alias { get; init; }

    /// <summary>
    /// Gets or inits the join type.
    /// </summary>
    public required IJoinType JoinType { get; init; }

    /// <summary>
    /// Gets or inits the join conditions.
    /// </summary>
    public required IReadOnlyList<(string LeftField, string RightField)> Conditions { get; init; }
}
