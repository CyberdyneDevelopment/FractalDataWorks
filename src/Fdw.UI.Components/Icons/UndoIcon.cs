using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Undo the last edit.</summary>
[TypeOption(typeof(IconGlyphs), "Undo")]
[ExcludeFromCodeCoverage]
public sealed class UndoIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="UndoIcon"/>.</summary>
    public UndoIcon()
        : base(
            44,
            "Undo",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M3 10h10a8 8 0 018 8v2M3 10l6 6m-6-6l6-6"
            ])
    { }
}
