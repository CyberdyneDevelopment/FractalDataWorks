using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>The node is operating but has warnings.</summary>
[TypeOption(typeof(LineageNodeStatuses), "Warning")]
[ExcludeFromCodeCoverage]
public sealed class WarningLineageNodeStatus : LineageNodeStatusBase
{
    /// <summary>Initializes a new instance of <see cref="WarningLineageNodeStatus"/>.</summary>
    public WarningLineageNodeStatus() : base(3, "Warning") { }
}
