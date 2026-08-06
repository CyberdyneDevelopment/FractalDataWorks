using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.NodeTypes;

/// <summary>
/// A connection to an external data store.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageNodeTypes), "Connection")]
public sealed class ConnectionNodeType : LineageNodeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionNodeType"/> class.
    /// </summary>
    public ConnectionNodeType() : base(3, "Connection") { }
}
