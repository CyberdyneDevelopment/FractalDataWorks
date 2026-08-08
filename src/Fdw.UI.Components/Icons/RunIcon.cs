using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Run a job, compile, or test execution.</summary>
[TypeOption(typeof(IconGlyphs), "Run")]
[ExcludeFromCodeCoverage]
public sealed class RunIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="RunIcon"/>.</summary>
    public RunIcon()
        : base(
            12,
            "Run",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z",
                "M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            ])
    { }
}
