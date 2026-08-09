using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Expand a collapsed section.</summary>
[TypeOption(typeof(IconGlyphs), "ChevronDown")]
[ExcludeFromCodeCoverage]
public sealed class ChevronDownIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="ChevronDownIcon"/>.</summary>
    public ChevronDownIcon()
        : base(
            13,
            "ChevronDown",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M19 9l-7 7-7-7"
            ])
    { }
}
