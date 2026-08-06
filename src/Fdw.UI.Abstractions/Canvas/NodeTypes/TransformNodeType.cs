using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas.NodeTypes;

/// <summary>
/// A transformation step node (filter, map, aggregate, lookup, etc.).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CanvasNodeTypes), "Transform")]
public sealed class TransformNodeType : CanvasNodeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransformNodeType"/> class.
    /// </summary>
    public TransformNodeType()
        : base(5, "Transform", "Transform", "Processing", "shuffle")
    {
    }
}
