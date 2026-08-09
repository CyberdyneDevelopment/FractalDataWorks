using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Search within a list, on the heavier magnifier.</summary>
[TypeOption(typeof(IconGlyphs), "SearchAlt")]
[ExcludeFromCodeCoverage]
public sealed class SearchAltIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="SearchAltIcon"/>.</summary>
    public SearchAltIcon()
        : base(
            32,
            "SearchAlt",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
            ])
    { }
}
