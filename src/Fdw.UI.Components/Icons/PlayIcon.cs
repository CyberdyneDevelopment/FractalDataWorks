using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Execute a query or start a run.</summary>
[TypeOption(typeof(IconGlyphs), "Play")]
[ExcludeFromCodeCoverage]
public sealed class PlayIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="PlayIcon"/>.</summary>
    public PlayIcon()
        : base(
            15,
            "Play",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z"
            ])
    { }
}
