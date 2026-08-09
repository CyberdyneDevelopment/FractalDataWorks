using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Disclose or advance to the next item.</summary>
[TypeOption(typeof(IconGlyphs), "ChevronRight")]
[ExcludeFromCodeCoverage]
public sealed class ChevronRightIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="ChevronRightIcon"/>.</summary>
    public ChevronRightIcon()
        : base(
            5,
            "ChevronRight",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M9 5l7 7-7 7"
            ])
    { }
}
