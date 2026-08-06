using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas.NodeTypes;

/// <summary>
/// An ETL/ELT pipeline node representing a complete data movement pipeline.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CanvasNodeTypes), "Pipeline")]
public sealed class PipelineNodeType : CanvasNodeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineNodeType"/> class.
    /// </summary>
    public PipelineNodeType()
        : base(6, "Pipeline", "Pipeline", "Processing", "git-branch")
    {
    }
}
