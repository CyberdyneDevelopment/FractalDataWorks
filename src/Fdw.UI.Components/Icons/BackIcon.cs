using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Navigate back to the owning list.</summary>
[TypeOption(typeof(IconGlyphs), "Back")]
[ExcludeFromCodeCoverage]
public sealed class BackIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="BackIcon"/>.</summary>
    public BackIcon()
        : base(
            10,
            "Back",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M10 19l-7-7m0 0l7-7m-7 7h18"
            ])
    { }
}
