using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas.NodeTypes;

/// <summary>
/// A calculation graph operation step node that performs a computation on its inputs.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CanvasNodeTypes), "CalcOperation")]
public sealed class CalcOperationNodeType : CanvasNodeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalcOperationNodeType"/> class.
    /// </summary>
    public CalcOperationNodeType()
        : base(9, "CalcOperation", "Calc Operation", "Calculation", "cpu")
    {
    }
}
