using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Flow from one thing to the next.</summary>
[TypeOption(typeof(IconGlyphs), "ArrowRight")]
[ExcludeFromCodeCoverage]
public sealed class ArrowRightIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="ArrowRightIcon"/>.</summary>
    public ArrowRightIcon()
        : base(
            19,
            "ArrowRight",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M17 8l4 4m0 0l-4 4m4-4H3"
            ])
    { }
}
