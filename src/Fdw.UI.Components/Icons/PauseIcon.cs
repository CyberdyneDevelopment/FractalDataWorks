using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Pause a running execution.</summary>
[TypeOption(typeof(IconGlyphs), "Pause")]
[ExcludeFromCodeCoverage]
public sealed class PauseIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="PauseIcon"/>.</summary>
    public PauseIcon()
        : base(
            25,
            "Pause",
            "0 0 24 24",
            "currentColor",
            "",
            "",
            false,
            [
                "M6 19h4V5H6v14zm8-14v14h4V5h-4z"
            ])
    { }
}
