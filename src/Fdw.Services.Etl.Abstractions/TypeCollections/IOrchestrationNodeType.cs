using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

/// <summary>
/// Interface for orchestration node types in the recursive hierarchy.
/// Each type carries metadata about where it may appear in the tree and what it may contain.
/// </summary>
public interface IOrchestrationNodeType : ITypeOption<int, OrchestrationNodeTypeBase>
{
    /// <summary>Gets the typical depth of this node type (0 = root).</summary>
    int TypicalDepth { get; }

    /// <summary>Gets a value indicating whether this node type may appear at the tree root (no parent).</summary>
    bool CanBeRoot { get; }

    /// <summary>
    /// Gets a value indicating whether this node type may host pipeline memberships.
    /// Only leaf-level types host pipelines.
    /// </summary>
    bool CanHostPipelines { get; }

    /// <summary>
    /// Gets the set of node type names that may appear as direct children of this node type,
    /// or <see langword="null"/> if any child type is allowed.
    /// </summary>
    IReadOnlyList<string>? AllowedChildTypeNames { get; }
}
