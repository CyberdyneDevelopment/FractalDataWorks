using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Execution;

/// <summary>
/// Job - a discrete unit of work within a workflow.
/// </summary>
[TypeOption(typeof(ExecutionItemTypes), "Job", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class JobItemType : ExecutionItemTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobItemType"/> class.
    /// </summary>
    public JobItemType()
        : base(
            id: 2,
            name: "Job",
            displayName: "Job",
            hierarchyLevel: 1,
            canHaveChildren: true,
            canHaveParent: true)
    {
    }
}