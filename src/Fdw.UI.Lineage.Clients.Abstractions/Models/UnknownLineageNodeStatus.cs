using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>The status of the node is not known.</summary>
[TypeOption(typeof(LineageNodeStatuses), "Unknown")]
[ExcludeFromCodeCoverage]
public sealed class UnknownLineageNodeStatus : LineageNodeStatusBase
{
    /// <summary>Initializes a new instance of <see cref="UnknownLineageNodeStatus"/>.</summary>
    public UnknownLineageNodeStatus() : base(1, "Unknown") { }
}
