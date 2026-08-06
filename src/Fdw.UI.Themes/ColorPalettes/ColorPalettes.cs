using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Themes;

/// <summary>
/// TypeCollection of available color palettes.
/// </summary>
/// <remarks>
/// Access palettes by name: <c>ColorPalettes.ByName("Dark")</c>
/// Access palettes by id: <c>ColorPalettes.ById(1)</c>
/// Get all palettes: <c>ColorPalettes.All()</c>
/// </remarks>
[TypeCollection(typeof(ColorPaletteBase), typeof(IColorPalette), typeof(ColorPalettes))]
public partial class ColorPalettes : TypeCollectionBase<ColorPaletteBase, IColorPalette>
{
}
