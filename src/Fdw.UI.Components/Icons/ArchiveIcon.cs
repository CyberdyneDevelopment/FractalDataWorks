using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Archive a message.</summary>
[TypeOption(typeof(IconGlyphs), "Archive")]
[ExcludeFromCodeCoverage]
public sealed class ArchiveIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="ArchiveIcon"/>.</summary>
    public ArchiveIcon()
        : base(
            20,
            "Archive",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M5 8h14M5 8a2 2 0 110-4h14a2 2 0 110 4M5 8v10a2 2 0 002 2h10a2 2 0 002-2V8m-9 4h4"
            ])
    { }
}
