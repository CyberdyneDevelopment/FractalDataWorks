using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>The source node consumes data produced by the target node.</summary>
[TypeOption(typeof(LineageEdgeTypes), "Consumes")]
[ExcludeFromCodeCoverage]
public sealed class ConsumesLineageEdgeType : LineageEdgeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="ConsumesLineageEdgeType"/>.</summary>
    public ConsumesLineageEdgeType() : base(2, "Consumes") { }
}
