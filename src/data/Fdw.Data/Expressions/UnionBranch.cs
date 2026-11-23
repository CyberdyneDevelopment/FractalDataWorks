using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>One more container whose rows a query stacks under its own, as a <c>UNION ALL</c> branch (FDW-6946).</summary>
public sealed class UnionBranch : IUnionBranch
{
    /// <inheritdoc />
    public required string ContainerName { get; init; }

    /// <inheritdoc />
    public required IReadOnlyList<ProjectionField> Fields { get; init; }
}
