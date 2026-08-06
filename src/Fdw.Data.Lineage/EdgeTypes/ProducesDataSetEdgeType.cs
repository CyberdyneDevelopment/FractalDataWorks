using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.EdgeTypes;

/// <summary>
/// Calculation produces a DataSet (Calculation→DataSet).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageEdgeTypes), "ProducesDataSet")]
public sealed class ProducesDataSetEdgeType : LineageEdgeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProducesDataSetEdgeType"/> class.
    /// </summary>
    public ProducesDataSetEdgeType() : base(6, "ProducesDataSet") { }
}
