using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Reload the current data.</summary>
[TypeOption(typeof(IconGlyphs), "Refresh")]
[ExcludeFromCodeCoverage]
public sealed class RefreshIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="RefreshIcon"/>.</summary>
    public RefreshIcon()
        : base(
            4,
            "Refresh",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
            ])
    { }
}
