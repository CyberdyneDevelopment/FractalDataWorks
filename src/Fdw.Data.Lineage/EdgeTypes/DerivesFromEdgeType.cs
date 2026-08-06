using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.EdgeTypes;

/// <summary>
/// A DataSet derives its data from another DataSet (source DataSet → derived DataSet).
/// Used for: DataSet → DerivesFrom → DataSet (from data.DataSetSource.SourceDataSetName).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageEdgeTypes), "DerivesFrom")]
public sealed class DerivesFromEdgeType : LineageEdgeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DerivesFromEdgeType"/> class.</summary>
    public DerivesFromEdgeType() : base(10, "DerivesFrom") { }
}
