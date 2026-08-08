using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Edit a record.</summary>
[TypeOption(typeof(IconGlyphs), "Edit")]
[ExcludeFromCodeCoverage]
public sealed class EditIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="EditIcon"/>.</summary>
    public EditIcon()
        : base(
            9,
            "Edit",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
            ])
    { }
}
