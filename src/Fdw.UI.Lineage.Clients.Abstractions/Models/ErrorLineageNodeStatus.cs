using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>The node has encountered an error.</summary>
[TypeOption(typeof(LineageNodeStatuses), "Error")]
[ExcludeFromCodeCoverage]
public sealed class ErrorLineageNodeStatus : LineageNodeStatusBase
{
    /// <summary>Initializes a new instance of <see cref="ErrorLineageNodeStatus"/>.</summary>
    public ErrorLineageNodeStatus() : base(4, "Error") { }
}
