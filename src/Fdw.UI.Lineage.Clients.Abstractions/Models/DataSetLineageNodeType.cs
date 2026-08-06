using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>The node represents a data set.</summary>
[TypeOption(typeof(LineageNodeTypes), "DataSet")]
[ExcludeFromCodeCoverage]
public sealed class DataSetLineageNodeType : LineageNodeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="DataSetLineageNodeType"/>.</summary>
    public DataSetLineageNodeType() : base(2, "DataSet") { }
}
