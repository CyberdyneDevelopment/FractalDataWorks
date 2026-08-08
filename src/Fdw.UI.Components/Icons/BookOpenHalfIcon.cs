using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>A catalogue or record count.</summary>
[TypeOption(typeof(IconGlyphs), "BookOpenHalf")]
[ExcludeFromCodeCoverage]
public sealed class BookOpenHalfIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="BookOpenHalfIcon"/>.</summary>
    public BookOpenHalfIcon()
        : base(
            51,
            "BookOpenHalf",
            "0 0 24 24",
            "none",
            "currentColor",
            "",
            true,
            [
                "M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253"
            ])
    { }
}
