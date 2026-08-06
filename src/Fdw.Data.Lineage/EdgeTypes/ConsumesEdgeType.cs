using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.EdgeTypes;

/// <summary>
/// Pipeline consumes a DataSet.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageEdgeTypes), "Consumes")]
public sealed class ConsumesEdgeType : LineageEdgeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumesEdgeType"/> class.
    /// </summary>
    public ConsumesEdgeType() : base(2, "Consumes") { }
}
