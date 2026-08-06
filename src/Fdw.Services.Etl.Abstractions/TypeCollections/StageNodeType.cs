using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

/// <summary>
/// Stage — a phase within a Project. Contains Step (or SubStep) children.
/// Maps to NodeTypeId=2 in pipe.OrchestrationNode.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(OrchestrationNodeTypes), "Stage")]
public sealed class StageNodeType : OrchestrationNodeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="StageNodeType"/>.</summary>
    public StageNodeType()
        : base(
            id: 2,
            name: "Stage",
            displayName: "Stage",
            typicalDepth: 1,
            canBeRoot: false,
            canHostPipelines: false,
            allowedChildTypeNames: ["Step", "SubStep"])
    {
    }
}
