using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Restore the panel from fullscreen.</summary>
[TypeOption(typeof(IconGlyphs), "FullscreenExit")]
[ExcludeFromCodeCoverage]
public sealed class FullscreenExitIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="FullscreenExitIcon"/>.</summary>
    public FullscreenExitIcon()
        : base(
            47,
            "FullscreenExit",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M9 9L4 4m0 0l5 0m-5 0l0 5m11 1l5 5m0 0l-5 0m5 0l0-5"
            ])
    { }
}
