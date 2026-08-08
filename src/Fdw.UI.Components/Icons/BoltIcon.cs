using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Throughput or activity rate.</summary>
[TypeOption(typeof(IconGlyphs), "Bolt")]
[ExcludeFromCodeCoverage]
public sealed class BoltIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="BoltIcon"/>.</summary>
    public BoltIcon()
        : base(
            49,
            "Bolt",
            "0 0 24 24",
            "none",
            "currentColor",
            "",
            true,
            [
                "M13 10V3L4 14h7v7l9-11h-7z"
            ])
    { }
}
