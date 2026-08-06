using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>The node data is outdated or has not been refreshed recently.</summary>
[TypeOption(typeof(LineageNodeStatuses), "Stale")]
[ExcludeFromCodeCoverage]
public sealed class StaleLineageNodeStatus : LineageNodeStatusBase
{
    /// <summary>Initializes a new instance of <see cref="StaleLineageNodeStatus"/>.</summary>
    public StaleLineageNodeStatus() : base(5, "Stale") { }
}
