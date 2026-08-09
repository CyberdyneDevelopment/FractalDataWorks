using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Open in a new context.</summary>
[TypeOption(typeof(IconGlyphs), "ExternalLink")]
[ExcludeFromCodeCoverage]
public sealed class ExternalLinkIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="ExternalLinkIcon"/>.</summary>
    public ExternalLinkIcon()
        : base(
            41,
            "ExternalLink",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"
            ])
    { }
}
