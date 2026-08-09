using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Dismiss a dialog, drawer, or inline panel.</summary>
[TypeOption(typeof(IconGlyphs), "Close")]
[ExcludeFromCodeCoverage]
public sealed class CloseIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="CloseIcon"/>.</summary>
    public CloseIcon()
        : base(
            1,
            "Close",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M6 18L18 6M6 6l12 12"
            ])
    { }
}
