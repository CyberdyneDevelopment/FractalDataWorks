using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.NodeTypes;

/// <summary>
/// A calculation node (transforms or aggregates data).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageNodeTypes), "Calculation")]
public sealed class CalculationNodeType : LineageNodeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationNodeType"/> class.
    /// </summary>
    public CalculationNodeType() : base(5, "Calculation") { }
}
