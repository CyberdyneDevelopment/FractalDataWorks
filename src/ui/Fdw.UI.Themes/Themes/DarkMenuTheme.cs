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
        name: "Dark",
        themeFamily: "Dark",
        colors: ColorPalettes.Dark,
        borders: BorderStyles.Rounded,
        icons: IconSets.Unicode) { }
}
