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
        name: "Light",
        themeFamily: "Light",
        colors: ColorPalettes.Light,
        borders: BorderStyles.Rounded,
        icons: IconSets.Unicode) { }
}
