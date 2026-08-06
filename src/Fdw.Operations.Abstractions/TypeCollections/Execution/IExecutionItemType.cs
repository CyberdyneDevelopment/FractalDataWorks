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
    /// Determines whether this execution item type can contain the specified child type.
    /// </summary>
    /// <param name="childType">The potential child type.</param>
    /// <returns><c>true</c> if this type can contain the child type; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// Flexible containment rules:
    /// <list type="bullet">
    ///   <item><description>A type can contain any type at a lower hierarchy level</description></item>
    ///   <item><description>Types cannot contain themselves or types at the same level</description></item>
    ///   <item><description>Types cannot contain types at a higher level (e.g., Task cannot contain Workflow)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    bool CanContain(IExecutionItemType childType);
}
