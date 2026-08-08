using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Search within a list.</summary>
[TypeOption(typeof(IconGlyphs), "Search")]
[ExcludeFromCodeCoverage]
public sealed class SearchIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="SearchIcon"/>.</summary>
    public SearchIcon()
        : base(
            29,
            "Search",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M21 21l-4.3-4.3M11 18a7 7 0 100-14 7 7 0 000 14z"
            ])
    { }
}
