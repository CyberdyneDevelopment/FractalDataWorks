using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Themes;

/// <summary>
/// High contrast menu theme for accessibility.
/// </summary>
/// <remarks>
/// Uses high contrast colors and ASCII borders for maximum readability.
/// Designed for users with visual impairments or terminals with limited capabilities.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MenuThemes), "HighContrast", RestrictToCurrentCompilation = true)]
public sealed class HighContrastMenuTheme : MenuThemeBase
{
    /// <summary>
    /// Creates the high contrast menu theme.
    /// </summary>
    public HighContrastMenuTheme() : base(
        id: 3,
        name: "HighContrast",
        themeFamily: "HighContrast",
        colorPaletteId: 3,
        borderStyleId: 3,
        iconSetId: 2,
        colors: ColorPalettes.ById(3),
        borders: BorderStyles.ById(3),
        icons: IconSets.ById(2),
        paddingSmall: 1,
        paddingMedium: 2,
        paddingLarge: 3) { }
}
