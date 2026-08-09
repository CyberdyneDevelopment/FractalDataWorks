using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Return to the previous item, on the tighter chevron.</summary>
[TypeOption(typeof(IconGlyphs), "ChevronLeftAlt")]
[ExcludeFromCodeCoverage]
public sealed class ChevronLeftAltIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="ChevronLeftAltIcon"/>.</summary>
    public ChevronLeftAltIcon()
        : base(
            43,
            "ChevronLeftAlt",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M15 18l-6-6 6-6"
            ])
    { }
}
