using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Skip the current step.</summary>
[TypeOption(typeof(IconGlyphs), "Skip")]
[ExcludeFromCodeCoverage]
public sealed class SkipIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="SkipIcon"/>.</summary>
    public SkipIcon()
        : base(
            26,
            "Skip",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M13 9l3 3m0 0l-3 3m3-3H8m13 0a9 9 0 11-18 0 9 9 0 0118 0z"
            ])
    { }
}
