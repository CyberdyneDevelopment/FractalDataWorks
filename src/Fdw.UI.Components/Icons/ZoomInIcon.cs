using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Zoom the canvas in.</summary>
[TypeOption(typeof(IconGlyphs), "ZoomIn")]
[ExcludeFromCodeCoverage]
public sealed class ZoomInIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="ZoomInIcon"/>.</summary>
    public ZoomInIcon()
        : base(
            22,
            "ZoomIn",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0zM10 7v6m3-3H7"
            ])
    { }
}
