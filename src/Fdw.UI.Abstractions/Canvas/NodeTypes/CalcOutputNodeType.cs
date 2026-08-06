using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas.NodeTypes;

/// <summary>
/// A calculation graph output result node that captures the final computed value.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CanvasNodeTypes), "CalcOutput")]
public sealed class CalcOutputNodeType : CanvasNodeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalcOutputNodeType"/> class.
    /// </summary>
    public CalcOutputNodeType()
        : base(10, "CalcOutput", "Calc Output", "Calculation", "log-out")
    {
    }
}
