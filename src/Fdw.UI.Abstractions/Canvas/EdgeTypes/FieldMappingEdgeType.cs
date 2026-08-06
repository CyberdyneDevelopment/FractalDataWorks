using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas.EdgeTypes;

/// <summary>
/// A field-level data mapping edge connecting source and target ports on adjacent nodes.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CanvasEdgeTypes), "FieldMapping")]
public sealed class FieldMappingEdgeType : CanvasEdgeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldMappingEdgeType"/> class.
    /// </summary>
    public FieldMappingEdgeType()
        : base(3, "FieldMapping", "Field Mapping", "git-merge")
    {
    }
}
