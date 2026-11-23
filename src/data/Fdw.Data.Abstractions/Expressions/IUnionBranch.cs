using System.Collections.Generic;

namespace Fdw.Data.Abstractions;

/// <summary>
/// One more container whose rows a query stacks under its own (FDW-6946): a <c>UNION ALL</c> branch. The query's own projection over its own
/// container is the first branch; each branch gives the same aliases, in the same order.
/// </summary>
public interface IUnionBranch
{
    /// <summary>Gets the container the branch reads, found by name among the containers of the query container's data store.</summary>
    string ContainerName { get; }

    /// <summary>Gets the branch's columns: each a column of its container (<see cref="ProjectionField.PropertyName"/>) given the alias the first branch gives it.</summary>
    IReadOnlyList<ProjectionField> Fields { get; }
}
