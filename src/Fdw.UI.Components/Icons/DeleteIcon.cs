using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Delete a record.</summary>
[TypeOption(typeof(IconGlyphs), "Delete")]
[ExcludeFromCodeCoverage]
public sealed class DeleteIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="DeleteIcon"/>.</summary>
    public DeleteIcon()
        : base(
            7,
            "Delete",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
            ])
    { }
}
