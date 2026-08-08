using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Add a new item, on the heavier stroke used by list toolbars.</summary>
[TypeOption(typeof(IconGlyphs), "AddAlt")]
[ExcludeFromCodeCoverage]
public sealed class AddAltIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="AddAltIcon"/>.</summary>
    public AddAltIcon()
        : base(
            8,
            "AddAlt",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M12 4v16m8-8H4"
            ])
    { }
}
