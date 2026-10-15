using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>The node represents a configured data store.</summary>
[TypeOption(typeof(LineageNodeTypes), "DataStore")]
[ExcludeFromCodeCoverage]
public sealed class DataStoreLineageNodeType : LineageNodeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="DataStoreLineageNodeType"/>.</summary>
    public DataStoreLineageNodeType() : base("DataStore") { }
}
