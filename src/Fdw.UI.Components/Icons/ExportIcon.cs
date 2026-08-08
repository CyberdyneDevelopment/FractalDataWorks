using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Export the current result set.</summary>
[TypeOption(typeof(IconGlyphs), "Export")]
[ExcludeFromCodeCoverage]
public sealed class ExportIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="ExportIcon"/>.</summary>
    public ExportIcon()
        : base(
            28,
            "Export",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"
            ])
    { }
}
