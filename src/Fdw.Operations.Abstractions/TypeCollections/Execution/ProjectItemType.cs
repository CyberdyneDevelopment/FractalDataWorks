using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Execution;

/// <summary>
/// Project — the root execution item type for project orchestration.
/// Peer of <see cref="WorkflowItemType"/>; both are top-level types (no parent, can have children).
/// A Project directly contains Stage items (skipping the Job level).
/// </summary>
/// <remarks>
/// The Project hierarchy is: Project → Stage → Step → Task (Pipeline runs tracked as Task).
/// This diverges from the Workflow hierarchy (Workflow → Job → Stage → Step → Task)
/// because Projects have a fixed semantic structure defined by their configuration.
/// </remarks>
[TypeOption(typeof(ExecutionItemTypes), "Project", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ProjectItemType : ExecutionItemTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectItemType"/> class.
    /// </summary>
    public ProjectItemType()
        : base(
            id: 6,
            name: "Project",
            displayName: "Project",
            hierarchyLevel: 0,
            canHaveChildren: true,
            canHaveParent: false)
    {
    }
}
