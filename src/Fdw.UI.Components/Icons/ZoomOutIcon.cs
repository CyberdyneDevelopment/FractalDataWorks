using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Zoom the canvas out.</summary>
[TypeOption(typeof(IconGlyphs), "ZoomOut")]
[ExcludeFromCodeCoverage]
public sealed class ZoomOutIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="ZoomOutIcon"/>.</summary>
    public ZoomOutIcon()
        : base(
            23,
            "ZoomOut",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0zM13 10H7"
            ])
    { }
}
