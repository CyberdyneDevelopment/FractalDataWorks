using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>The node is operating normally.</summary>
[TypeOption(typeof(LineageNodeStatuses), "Healthy")]
[ExcludeFromCodeCoverage]
public sealed class HealthyLineageNodeStatus : LineageNodeStatusBase
{
    /// <summary>Initializes a new instance of <see cref="HealthyLineageNodeStatus"/>.</summary>
    public HealthyLineageNodeStatus() : base(2, "Healthy") { }
}
