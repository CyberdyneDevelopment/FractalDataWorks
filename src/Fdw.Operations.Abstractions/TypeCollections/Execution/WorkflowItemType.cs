using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Execution;

/// <summary>
/// Workflow - the root execution item type.
/// </summary>
[TypeOption(typeof(ExecutionItemTypes), "Workflow", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkflowItemType : ExecutionItemTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowItemType"/> class.
    /// </summary>
    public WorkflowItemType()
        : base(
            id: 1,
            name: "Workflow",
            displayName: "Workflow",
            hierarchyLevel: 0,
            canHaveChildren: true,
            canHaveParent: false)
    {
    }
}