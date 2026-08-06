using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas.NodeTypes;

/// <summary>
/// A calculation graph input parameter node that receives a value fed into the calculation.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CanvasNodeTypes), "CalcInput")]
public sealed class CalcInputNodeType : CanvasNodeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalcInputNodeType"/> class.
    /// </summary>
    public CalcInputNodeType()
        : base(8, "CalcInput", "Calc Input", "Calculation", "log-in")
    {
    }
}
