using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Execution;

/// <summary>
/// Step - an individual action within a stage.
/// </summary>
[TypeOption(typeof(ExecutionItemTypes), "Step", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StepItemType : ExecutionItemTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StepItemType"/> class.
    /// </summary>
    public StepItemType()
        : base(
            id: 4,
            name: "Step",
            displayName: "Step",
            hierarchyLevel: 3,
            canHaveChildren: true,
            canHaveParent: true)
    {
    }
}