using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Security, roles, and permissions.</summary>
[TypeOption(typeof(IconGlyphs), "Shield")]
[ExcludeFromCodeCoverage]
public sealed class ShieldIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="ShieldIcon"/>.</summary>
    public ShieldIcon()
        : base(
            16,
            "Shield",
            "0 0 24 24",
            "none",
            "currentColor",
            "1.6",
            true,
            [
                "M12 3l8 4v5c0 5-4 8-8 9-4-1-8-4-8-9V7z"
            ])
    { }
}
