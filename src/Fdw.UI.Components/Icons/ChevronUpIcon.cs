using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Collapse an expanded section.</summary>
[TypeOption(typeof(IconGlyphs), "ChevronUp")]
[ExcludeFromCodeCoverage]
public sealed class ChevronUpIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="ChevronUpIcon"/>.</summary>
    public ChevronUpIcon()
        : base(
            48,
            "ChevronUp",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            false,
            [
                "M5 15l7-7 7 7"
            ])
    { }
}
