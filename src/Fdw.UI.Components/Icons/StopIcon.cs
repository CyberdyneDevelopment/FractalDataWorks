using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Stop a running execution.</summary>
[TypeOption(typeof(IconGlyphs), "Stop")]
[ExcludeFromCodeCoverage]
public sealed class StopIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="StopIcon"/>.</summary>
    public StopIcon()
        : base(
            27,
            "Stop",
            "0 0 24 24",
            "currentColor",
            "",
            "",
            false,
            [
                "M6 6h12v12H6z"
            ])
    { }
}
