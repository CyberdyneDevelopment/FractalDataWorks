using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas.EdgeTypes;

/// <summary>
/// Primary data or control-flow edge connecting pipeline steps in execution order.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CanvasEdgeTypes), "Flow")]
public sealed class FlowEdgeType : CanvasEdgeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FlowEdgeType"/> class.
    /// </summary>
    public FlowEdgeType()
        : base(1, "Flow", "Flow", "arrow-right")
    {
    }
}
