using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Themes;

/// <summary>
/// Dark menu theme - default theme for terminal UIs.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MenuThemes), "Dark", RestrictToCurrentCompilation = true)]
public sealed class DarkMenuTheme : MenuThemeBase
{
    /// <summary>
    /// Creates the dark menu theme.
    /// </summary>
    public DarkMenuTheme() : base(
        id: 1,
        name: "Dark",
        themeFamily: "Dark",
        colorPaletteId: 1,
        borderStyleId: 1,
        iconSetId: 1,
        colors: ColorPalettes.ById(1),
        borders: BorderStyles.ById(1),
        icons: IconSets.ById(1)) { }
}
