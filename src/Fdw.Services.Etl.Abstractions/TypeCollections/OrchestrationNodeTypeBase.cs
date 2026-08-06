using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

/// <summary>
/// Base class for orchestration node types using the CRTP pattern.
/// All values are passed via constructor arguments — no overrides allowed.
/// </summary>
public abstract class OrchestrationNodeTypeBase : TypeOptionBase<int, OrchestrationNodeTypeBase>, IOrchestrationNodeType
{
    /// <summary>Initializes the empty sentinel (NotFound).</summary>
    protected OrchestrationNodeTypeBase()
        : base(0, "NotFound", "TypeOptions:NotFound", "Not Found", "Unknown orchestration node type", null)
    {
        TypicalDepth = -1;
        CanBeRoot = false;
        CanHostPipelines = false;
        AllowedChildTypeNames = null;
    }

    /// <summary>Initializes a new instance of <see cref="OrchestrationNodeTypeBase"/>.</summary>
    /// <param name="id">Unique integer discriminator stored in <c>pipe.OrchestrationNode.NodeTypeId</c>.</param>
    /// <param name="name">TypeCollection lookup name (matches TypeOption attribute).</param>
    /// <param name="displayName">Human-readable name for UI.</param>
    /// <param name="typicalDepth">Typical depth in the tree (0 = root).</param>
    /// <param name="canBeRoot">Whether this type may appear without a parent.</param>
    /// <param name="canHostPipelines">Whether this type may host pipeline memberships.</param>
    /// <param name="allowedChildTypeNames">Permitted child type names, or null for unrestricted.</param>
    protected OrchestrationNodeTypeBase(
        int id,
        string name,
        string displayName,
        int typicalDepth,
        bool canBeRoot,
        bool canHostPipelines,
        IReadOnlyList<string>? allowedChildTypeNames)
        : base(id, name, $"TypeOptions:{name}", displayName, $"Orchestration node type: {name}", null)
    {
        TypicalDepth = typicalDepth;
        CanBeRoot = canBeRoot;
        CanHostPipelines = canHostPipelines;
        AllowedChildTypeNames = allowedChildTypeNames;
    }

    /// <inheritdoc />
    public int TypicalDepth { get; }

    /// <inheritdoc />
    public bool CanBeRoot { get; }

    /// <inheritdoc />
    public bool CanHostPipelines { get; }

    /// <inheritdoc />
    public IReadOnlyList<string>? AllowedChildTypeNames { get; }
}
