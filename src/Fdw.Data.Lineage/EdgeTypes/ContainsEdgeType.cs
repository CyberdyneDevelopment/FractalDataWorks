using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.EdgeTypes;

/// <summary>
/// A parent orchestration node contains a child node.
/// Used for: Project → Contains → Stage, Stage → Contains → Step, Step → Contains → Pipeline.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageEdgeTypes), "Contains")]
public sealed class ContainsEdgeType : LineageEdgeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainsEdgeType"/> class.</summary>
    public ContainsEdgeType() : base(7, "Contains") { }
}
