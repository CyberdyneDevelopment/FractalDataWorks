using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>A tabular data set.</summary>
[TypeOption(typeof(IconGlyphs), "Table")]
[ExcludeFromCodeCoverage]
public sealed class TableIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="TableIcon"/>.</summary>
    public TableIcon()
        : base(
            35,
            "Table",
            "0 0 24 24",
            "none",
            "#d99a3f",
            "",
            true,
            [
                "M4 5h16v14H4zM4 10h16M10 10v9"
            ])
    { }
}
