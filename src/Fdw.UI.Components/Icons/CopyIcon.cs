using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Duplicate or derive from an existing record.</summary>
[TypeOption(typeof(IconGlyphs), "Copy")]
[ExcludeFromCodeCoverage]
public sealed class CopyIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="CopyIcon"/>.</summary>
    public CopyIcon()
        : base(
            11,
            "Copy",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"
            ])
    { }
}
