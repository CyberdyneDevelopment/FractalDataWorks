using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.NodeTypes;

/// <summary>
/// An ETL project stage node — an ordered phase within a Project.
/// Stage N+1 waits for all Steps of Stage N to complete before executing.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageNodeTypes), "Stage")]
public sealed class StageNodeType : LineageNodeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="StageNodeType"/> class.</summary>
    public StageNodeType() : base(7, "Stage") { }
}
