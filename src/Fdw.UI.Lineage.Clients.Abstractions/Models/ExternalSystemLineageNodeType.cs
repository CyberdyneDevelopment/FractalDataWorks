using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>The node represents an external system.</summary>
[TypeOption(typeof(LineageNodeTypes), "ExternalSystem")]
[ExcludeFromCodeCoverage]
public sealed class ExternalSystemLineageNodeType : LineageNodeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="ExternalSystemLineageNodeType"/>.</summary>
    public ExternalSystemLineageNodeType() : base(4, "ExternalSystem") { }
}
