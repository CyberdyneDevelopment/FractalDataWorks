using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Move data between two stores.</summary>
[TypeOption(typeof(IconGlyphs), "Transfer")]
[ExcludeFromCodeCoverage]
public sealed class TransferIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="TransferIcon"/>.</summary>
    public TransferIcon()
        : base(
            36,
            "Transfer",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4"
            ])
    { }
}
