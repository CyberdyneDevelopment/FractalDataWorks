using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Return to the previous item.</summary>
[TypeOption(typeof(IconGlyphs), "ChevronLeft")]
[ExcludeFromCodeCoverage]
public sealed class ChevronLeftIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="ChevronLeftIcon"/>.</summary>
    public ChevronLeftIcon()
        : base(
            6,
            "ChevronLeft",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M15 19l-7-7 7-7"
            ])
    { }
}
