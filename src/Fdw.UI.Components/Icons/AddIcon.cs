using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Add a new item.</summary>
[TypeOption(typeof(IconGlyphs), "Add")]
[ExcludeFromCodeCoverage]
public sealed class AddIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="AddIcon"/>.</summary>
    public AddIcon()
        : base(
            2,
            "Add",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M12 5v14M5 12h14"
            ])
    { }
}
