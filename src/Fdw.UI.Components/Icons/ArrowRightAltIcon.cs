using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Flow from one thing to the next, on the longer arrow.</summary>
[TypeOption(typeof(IconGlyphs), "ArrowRightAlt")]
[ExcludeFromCodeCoverage]
public sealed class ArrowRightAltIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="ArrowRightAltIcon"/>.</summary>
    public ArrowRightAltIcon()
        : base(
            39,
            "ArrowRightAlt",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M14 5l7 7m0 0l-7 7m7-7H3"
            ])
    { }
}
