using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.NodeTypes;

/// <summary>
/// A configured data store: how users meet a connection, and what a pipeline reads from and writes to (FDW-1269).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageNodeTypes), "DataStore")]
public sealed class DataStoreNodeType : LineageNodeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreNodeType"/> class.
    /// </summary>
    public DataStoreNodeType() : base("DataStore") { }
}
