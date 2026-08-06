using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

/// <summary>
/// Project — the root orchestration node. Contains Stage children.
/// Maps to NodeTypeId=1 in pipe.OrchestrationNode.
/// </summary>
/// <remarks>
/// // Why: Id=1 matches v1 ExecutionItemTypes.Project for continuity with existing execution tracking records.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(OrchestrationNodeTypes), "Project")]
public sealed class ProjectNodeType : OrchestrationNodeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="ProjectNodeType"/>.</summary>
    public ProjectNodeType()
        : base(
            id: 1,
            name: "Project",
            displayName: "Project",
            typicalDepth: 0,
            canBeRoot: true,
            canHostPipelines: false,
            allowedChildTypeNames: ["Stage"])
    {
    }
}
