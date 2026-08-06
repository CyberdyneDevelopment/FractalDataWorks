using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>The node represents a data pipeline.</summary>
[TypeOption(typeof(LineageNodeTypes), "Pipeline")]
[ExcludeFromCodeCoverage]
public sealed class PipelineLineageNodeType : LineageNodeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="PipelineLineageNodeType"/>.</summary>
    public PipelineLineageNodeType() : base(1, "Pipeline") { }
}
