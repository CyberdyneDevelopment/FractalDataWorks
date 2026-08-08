using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>A physical server or data path.</summary>
[TypeOption(typeof(IconGlyphs), "Server")]
[ExcludeFromCodeCoverage]
public sealed class ServerIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="ServerIcon"/>.</summary>
    public ServerIcon()
        : base(
            38,
            "Server",
            "0 0 24 24",
            "none",
            "#d99a3f",
            "",
            true,
            [
                "M5 4h14v5H5zM5 11h14v5H5zM7 6.5h.01M7 13.5h.01"
            ])
    { }
}
