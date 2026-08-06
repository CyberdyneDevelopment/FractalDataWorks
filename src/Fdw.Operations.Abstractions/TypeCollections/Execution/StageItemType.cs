using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Execution;

/// <summary>
/// Stage - a phase within a job.
/// </summary>
[TypeOption(typeof(ExecutionItemTypes), "Stage", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StageItemType : ExecutionItemTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageItemType"/> class.
    /// </summary>
    public StageItemType()
        : base(
            id: 3,
            name: "Stage",
            displayName: "Stage",
            hierarchyLevel: 2,
            canHaveChildren: true,
            canHaveParent: true)
    {
    }
}