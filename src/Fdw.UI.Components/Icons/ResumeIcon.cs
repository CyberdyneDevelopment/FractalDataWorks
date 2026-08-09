using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Resume a paused execution.</summary>
[TypeOption(typeof(IconGlyphs), "Resume")]
[ExcludeFromCodeCoverage]
public sealed class ResumeIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="ResumeIcon"/>.</summary>
    public ResumeIcon()
        : base(
            24,
            "Resume",
            "0 0 24 24",
            "currentColor",
            "",
            "",
            false,
            [
                "M8 5v14l11-7z"
            ])
    { }
}
