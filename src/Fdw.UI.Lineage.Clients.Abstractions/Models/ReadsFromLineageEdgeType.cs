using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>The source node reads from the target node.</summary>
[TypeOption(typeof(LineageEdgeTypes), "ReadsFrom")]
[ExcludeFromCodeCoverage]
public sealed class ReadsFromLineageEdgeType : LineageEdgeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="ReadsFromLineageEdgeType"/>.</summary>
    public ReadsFromLineageEdgeType() : base(3, "ReadsFrom") { }
}
