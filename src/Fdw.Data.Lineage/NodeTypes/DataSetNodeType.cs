using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.NodeTypes;

/// <summary>
/// A logical dataset node (can be produced/consumed by pipelines).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageNodeTypes), "DataSet")]
public sealed class DataSetNodeType : LineageNodeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetNodeType"/> class.
    /// </summary>
    public DataSetNodeType() : base(2, "DataSet") { }
}
