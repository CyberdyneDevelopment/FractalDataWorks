using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Edit a record, on the lighter stroke used by table row actions.</summary>
[TypeOption(typeof(IconGlyphs), "EditAlt")]
[ExcludeFromCodeCoverage]
public sealed class EditAltIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="EditAltIcon"/>.</summary>
    public EditAltIcon()
        : base(
            30,
            "EditAlt",
            "0 0 24 24",
            "none",
            "currentColor",
            "1.8",
            true,
            [
                "M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.4-9.4a2 2 0 113 3L12 16l-4 1 1-4 9.6-9.4z"
            ])
    { }
}
