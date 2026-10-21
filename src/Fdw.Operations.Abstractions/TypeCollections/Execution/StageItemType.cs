using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Execution;

/// <summary>
/// Stage - a phase within a job.
/// </summary>
/// <remarks>
/// A stage holds stages, as a job holds jobs: the record of a pipeline run is a stage for each stage number, and an action, a loop, each round of a
/// loop and the stages of a loop's body are stages inside the stage they run in (FDW-2176).
/// </remarks>
[TypeOption(typeof(ExecutionItemTypes), "Stage", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StageItemType : ExecutionItemTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageItemType"/> class.
    /// </summary>
    public StageItemType()
        : base(
            name: "Stage",
            displayName: "Stage",
            hierarchyLevel: 2,
            canHaveChildren: true,
            canHaveParent: true,
            canContainSelf: true)
    {
    }
}