using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas.EdgeTypes;

/// <summary>
/// A dependency or reference relationship edge used in lineage graphs to show origin or derivation.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CanvasEdgeTypes), "Reference")]
public sealed class ReferenceEdgeType : CanvasEdgeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceEdgeType"/> class.
    /// </summary>
    public ReferenceEdgeType()
        : base(2, "Reference", "Reference", "link")
    {
    }
}
