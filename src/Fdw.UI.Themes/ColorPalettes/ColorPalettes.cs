using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Themes;

/// <summary>
/// TypeCollection of available color palettes.
/// </summary>
/// <remarks>
/// Access palettes by name: <c>ColorPalettes.ByName("Dark")</c>
/// Access a palette by its own id: <c>ColorPalettes.ById(palette.Id)</c>
/// Get all palettes: <c>ColorPalettes.All()</c>
/// </remarks>
[TypeCollection(typeof(ColorPaletteBase), typeof(IColorPalette), typeof(ColorPalettes))]
public partial class ColorPalettes : TypeCollectionBase<ColorPaletteBase, IColorPalette>
{
}
