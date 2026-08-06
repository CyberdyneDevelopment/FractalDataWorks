using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>The source node writes to the target node.</summary>
[TypeOption(typeof(LineageEdgeTypes), "WritesTo")]
[ExcludeFromCodeCoverage]
public sealed class WritesToLineageEdgeType : LineageEdgeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="WritesToLineageEdgeType"/>.</summary>
    public WritesToLineageEdgeType() : base(4, "WritesTo") { }
}
