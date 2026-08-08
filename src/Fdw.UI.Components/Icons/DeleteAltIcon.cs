using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Delete a record, on the destructive-red row action.</summary>
[TypeOption(typeof(IconGlyphs), "DeleteAlt")]
[ExcludeFromCodeCoverage]
public sealed class DeleteAltIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="DeleteAltIcon"/>.</summary>
    public DeleteAltIcon()
        : base(
            17,
            "DeleteAlt",
            "0 0 24 24",
            "none",
            "#e05c4a",
            "1.8",
            true,
            [
                "M5 7h14M9 7V5a1 1 0 011-1h4a1 1 0 011 1v2m1 0v12a2 2 0 01-2 2H8a2 2 0 01-2-2V7"
            ])
    { }
}
