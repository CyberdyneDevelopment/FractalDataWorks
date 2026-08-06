using Fdw.Collections;

namespace Fdw.Operations.Abstractions.TypeCollections.Execution;

/// <summary>
/// Base class for execution item types using the CRTP pattern.
/// Defines the hierarchy: Workflow → Job → Stage → Step → Task.
/// </summary>
/// <remarks>
/// <para>
/// The hierarchy supports flexible containment: any type can contain any type at a lower hierarchy level.
/// This allows workflows to directly contain tasks without intermediate levels, or to use the full
/// hierarchy as needed.
/// </para>
/// </remarks>
public abstract class ExecutionItemTypeBase : TypeOptionBase<int, ExecutionItemTypeBase>, IExecutionItemType
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected ExecutionItemTypeBase()
        : base(0, "NotFound", "TypeOptions:NotFound", "Not Found", "Unknown execution item type", null)
    {
        HierarchyLevel = -1;
        CanHaveChildren = false;
        CanHaveParent = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionItemTypeBase"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for this type.</param>
    /// <param name="name">Name of the type (must match TypeOption attribute).</param>
    /// <param name="displayName">Display name for UI presentation.</param>
    /// <param name="hierarchyLevel">Level in the hierarchy (0 = root).</param>
    /// <param name="canHaveChildren">Whether this type can have children.</param>
    /// <param name="canHaveParent">Whether this type can have a parent.</param>
    protected ExecutionItemTypeBase(
        int id,
        string name,
        string displayName,
        int hierarchyLevel,
        bool canHaveChildren,
        bool canHaveParent)
        : base(id, name, $"TypeOptions:{name}", displayName, $"Execution item type: {name}", null)
    {
        HierarchyLevel = hierarchyLevel;
        CanHaveChildren = canHaveChildren;
        CanHaveParent = canHaveParent;
    }

    /// <inheritdoc />
    public int HierarchyLevel { get; }

    /// <inheritdoc />
    public bool CanHaveChildren { get; }

    /// <inheritdoc />
    public bool CanHaveParent { get; }

    /// <inheritdoc />
    public bool CanContain(IExecutionItemType childType)
    {
        // Cannot contain if this type doesn't support children
        if (!CanHaveChildren)
        {
            return false;
        }

        // Cannot contain if child type doesn't support having a parent
        if (childType == null || !childType.CanHaveParent)
        {
            return false;
        }

        // Flexible containment: any type can contain any type at a lower hierarchy level
        // This allows Workflow to directly contain Task, or use full hierarchy as needed
        return childType.HierarchyLevel > HierarchyLevel;
    }
}
