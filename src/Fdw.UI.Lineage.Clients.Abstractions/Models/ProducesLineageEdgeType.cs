using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>The source node produces data consumed by the target node.</summary>
[TypeOption(typeof(LineageEdgeTypes), "Produces")]
[ExcludeFromCodeCoverage]
public sealed class ProducesLineageEdgeType : LineageEdgeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="ProducesLineageEdgeType"/>.</summary>
    public ProducesLineageEdgeType() : base(1, "Produces") { }
}
