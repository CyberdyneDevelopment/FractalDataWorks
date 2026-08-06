using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas.NodeTypes;

/// <summary>
/// A calculation chain node that groups a set of calculation operations.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CanvasNodeTypes), "Calculation")]
public sealed class CalculationNodeType : CanvasNodeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationNodeType"/> class.
    /// </summary>
    public CalculationNodeType()
        : base(4, "Calculation", "Calculation", "Processing", "calculator")
    {
    }
}
