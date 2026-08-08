using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Elapsed or scheduled time.</summary>
[TypeOption(typeof(IconGlyphs), "Clock")]
[ExcludeFromCodeCoverage]
public sealed class ClockIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="ClockIcon"/>.</summary>
    public ClockIcon()
        : base(
            50,
            "Clock",
            "0 0 24 24",
            "none",
            "currentColor",
            "",
            true,
            [
                "M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
            ])
    { }
}
