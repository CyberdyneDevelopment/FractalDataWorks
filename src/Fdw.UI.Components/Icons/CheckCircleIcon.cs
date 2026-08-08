using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Approve or publish.</summary>
[TypeOption(typeof(IconGlyphs), "CheckCircle")]
[ExcludeFromCodeCoverage]
public sealed class CheckCircleIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="CheckCircleIcon"/>.</summary>
    public CheckCircleIcon()
        : base(
            21,
            "CheckCircle",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
            ])
    { }
}
