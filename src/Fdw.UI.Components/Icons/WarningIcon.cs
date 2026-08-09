using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>A quality or health warning.</summary>
[TypeOption(typeof(IconGlyphs), "Warning")]
[ExcludeFromCodeCoverage]
public sealed class WarningIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="WarningIcon"/>.</summary>
    public WarningIcon()
        : base(
            46,
            "Warning",
            "0 0 24 24",
            "none",
            "currentColor",
            "",
            true,
            [
                "M12 9v4m0 4h.01M10.3 3.9L2.4 18a1.5 1.5 0 001.3 2.2h16.6a1.5 1.5 0 001.3-2.2L13.7 3.9a1.5 1.5 0 00-2.6 0z"
            ])
    { }
}
