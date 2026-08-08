using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>An API key or secret.</summary>
[TypeOption(typeof(IconGlyphs), "Key")]
[ExcludeFromCodeCoverage]
public sealed class KeyIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="KeyIcon"/>.</summary>
    public KeyIcon()
        : base(
            37,
            "Key",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z"
            ])
    { }
}
