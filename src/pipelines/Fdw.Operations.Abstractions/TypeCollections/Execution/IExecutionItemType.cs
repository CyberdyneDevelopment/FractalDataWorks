using Fdw.Collections;

namespace Fdw.Operations.Abstractions.TypeCollections.Execution;

/// <summary>
/// Interface for execution item types in the hierarchy.
/// </summary>
/// <remarks>
/// <para>
/// The hierarchy supports flexible containment: any type can contain any type at a lower hierarchy level.
/// For example, a Workflow can directly contain a Task without intermediate Job/Stage/Step levels.
/// </para>
/// </remarks>
public interface IExecutionItemType : ITypeOption<int, ExecutionItemTypeBase>
{
    /// <summary>
    /// Gets a value indicating whether this type can have children.
    /// </summary>
    bool CanHaveChildren { get; }

    /// <summary>
    /// Gets a value indicating whether this type can have a parent.
    /// </summary>
    bool CanHaveParent { get; }

    /// <summary>
    /// Gets the hierarchy level (0 = root, higher = deeper).
    /// </summary>
    int HierarchyLevel { get; }

    /// <summary>
    /// Gets a value indicating whether an item of this type may contain items of its own type.
    /// </summary>
    /// <remarks>
    /// Why stated: containment otherwise needs a deeper level, so a run could never start a run of its own type. A Job (a
    /// pipeline run) states it contains Jobs, so one pipeline can start another; the tracker still refuses a child of a
    /// type that has no parent and a parent that has no children.
    /// </remarks>
    bool CanContainSelf { get; }

    /// <summary>
    /// Determines whether this execution item type can contain the specified child type.
    /// </summary>
    /// <param name="childType">The potential child type.</param>
    /// <returns><c>true</c> if this type can contain the child type; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// Flexible containment rules:
    /// <list type="bullet">
    ///   <item><description>A type can contain any type at a lower hierarchy level</description></item>
    ///   <item><description>Types cannot contain types at the same level, and cannot contain themselves unless <see cref="CanContainSelf"/> says so</description></item>
    ///   <item><description>Types cannot contain types at a higher level (e.g., Task cannot contain Workflow)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    bool CanContain(IExecutionItemType childType);
}
