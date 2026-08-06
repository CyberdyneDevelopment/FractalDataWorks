using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.NodeTypes;

/// <summary>
/// An ETL/ELT pipeline node.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageNodeTypes), "Pipeline")]
public sealed class PipelineNodeType : LineageNodeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineNodeType"/> class.
    /// </summary>
    public PipelineNodeType() : base(1, "Pipeline") { }
}
