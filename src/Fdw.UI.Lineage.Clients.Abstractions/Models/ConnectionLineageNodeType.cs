using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>The node represents a data connection.</summary>
[TypeOption(typeof(LineageNodeTypes), "Connection")]
[ExcludeFromCodeCoverage]
public sealed class ConnectionLineageNodeType : LineageNodeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="ConnectionLineageNodeType"/>.</summary>
    public ConnectionLineageNodeType() : base(3, "Connection") { }
}
