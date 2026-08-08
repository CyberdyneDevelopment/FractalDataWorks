using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Expand the panel to fill the viewport.</summary>
[TypeOption(typeof(IconGlyphs), "Fullscreen")]
[ExcludeFromCodeCoverage]
public sealed class FullscreenIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="FullscreenIcon"/>.</summary>
    public FullscreenIcon()
        : base(
            42,
            "Fullscreen",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4"
            ])
    { }
}
