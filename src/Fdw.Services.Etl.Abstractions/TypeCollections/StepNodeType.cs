using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

/// <summary>
/// Step — a leaf execution unit within a Stage. Hosts pipeline memberships.
/// Maps to NodeTypeId=3 in pipe.OrchestrationNode.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(OrchestrationNodeTypes), "Step")]
public sealed class StepNodeType : OrchestrationNodeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="StepNodeType"/>.</summary>
    public StepNodeType()
        : base(
            id: 3,
            name: "Step",
            displayName: "Step",
            typicalDepth: 2,
            canBeRoot: false,
            canHostPipelines: true,
            allowedChildTypeNames: ["SubStep"])
    {
    }
}
