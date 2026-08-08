using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Confirm a completed step.</summary>
[TypeOption(typeof(IconGlyphs), "Check")]
[ExcludeFromCodeCoverage]
public sealed class CheckIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="CheckIcon"/>.</summary>
    public CheckIcon()
        : base(
            3,
            "Check",
            "0 0 24 24",
            "none",
            "currentColor",
            "3",
            true,
            [
                "M5 13l4 4L19 7"
            ])
    { }
}
