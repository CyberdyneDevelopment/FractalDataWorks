using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Execution;

/// <summary>
/// Pipeline — a single ETL pipeline execution tracked in the execution system.
/// Routes to the ETL pipeline execution queue on trigger.
/// Sits at the leaf level of the execution hierarchy (no children).
/// </summary>
/// <remarks>
/// The Pipeline type allows the unified trigger endpoint to route "pipeline" requests
/// through the TypeCollection dispatch pattern without hardcoding the type name.
/// </remarks>
[TypeOption(typeof(ExecutionItemTypes), "Pipeline", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PipelineItemType : ExecutionItemTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="PipelineItemType"/> class.</summary>
    public PipelineItemType()
        : base(
            id: 10,
            name: "Pipeline",
            displayName: "Pipeline",
            hierarchyLevel: 4,
            canHaveChildren: false,
            canHaveParent: true)
    {
    }
}
