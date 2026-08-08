using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Redo the undone edit.</summary>
[TypeOption(typeof(IconGlyphs), "Redo")]
[ExcludeFromCodeCoverage]
public sealed class RedoIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="RedoIcon"/>.</summary>
    public RedoIcon()
        : base(
            45,
            "Redo",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M21 10h-10a8 8 0 00-8 8v2M21 10l-6 6m6-6l-6-6"
            ])
    { }
}
