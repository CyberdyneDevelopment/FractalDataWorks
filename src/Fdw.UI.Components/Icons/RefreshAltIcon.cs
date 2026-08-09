using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Reload, on the circular arrows used by dashboard headers.</summary>
[TypeOption(typeof(IconGlyphs), "RefreshAlt")]
[ExcludeFromCodeCoverage]
public sealed class RefreshAltIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="RefreshAltIcon"/>.</summary>
    public RefreshAltIcon()
        : base(
            14,
            "RefreshAlt",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M4 4v6h6M20 20v-6h-6M20 8a8 8 0 00-14-3M4 16a8 8 0 0014 3"
            ])
    { }
}
