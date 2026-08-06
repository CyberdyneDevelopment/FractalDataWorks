using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Execution;

/// <summary>
/// Task - the smallest trackable unit (leaf node).
/// </summary>
[TypeOption(typeof(ExecutionItemTypes), "Task", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TaskItemType : ExecutionItemTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskItemType"/> class.
    /// </summary>
    public TaskItemType()
        : base(
            id: 5,
            name: "Task",
            displayName: "Task",
            hierarchyLevel: 4,
            canHaveChildren: false,
            canHaveParent: true)
    {
    }
}