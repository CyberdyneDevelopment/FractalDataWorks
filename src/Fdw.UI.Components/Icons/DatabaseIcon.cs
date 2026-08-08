using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>A database or data store.</summary>
[TypeOption(typeof(IconGlyphs), "Database")]
[ExcludeFromCodeCoverage]
public sealed class DatabaseIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="DatabaseIcon"/>.</summary>
    public DatabaseIcon()
        : base(
            34,
            "Database",
            "0 0 24 24",
            "none",
            "currentColor",
            "",
            true,
            [
                "M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4"
            ])
    { }
}
