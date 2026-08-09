using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Persist the current edits.</summary>
[TypeOption(typeof(IconGlyphs), "Save")]
[ExcludeFromCodeCoverage]
public sealed class SaveIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="SaveIcon"/>.</summary>
    public SaveIcon()
        : base(
            18,
            "Save",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M8 7H5a2 2 0 00-2 2v9a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-3m-1 4l-3 3m0 0l-3-3m3 3V4"
            ])
    { }
}
