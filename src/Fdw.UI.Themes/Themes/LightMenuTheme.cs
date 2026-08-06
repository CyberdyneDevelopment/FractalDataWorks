using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Themes;

/// <summary>
/// Light menu theme for terminal UIs with light backgrounds.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MenuThemes), "Light", RestrictToCurrentCompilation = true)]
public sealed class LightMenuTheme : MenuThemeBase
{
    /// <summary>
    /// Creates the light menu theme.
    /// </summary>
    public LightMenuTheme() : base(
        id: 2,
        name: "Light",
        themeFamily: "Light",
        colorPaletteId: 2,
        borderStyleId: 1,
        iconSetId: 1,
        colors: ColorPalettes.ById(2),
        borders: BorderStyles.ById(1),
        icons: IconSets.ById(1)) { }
}
