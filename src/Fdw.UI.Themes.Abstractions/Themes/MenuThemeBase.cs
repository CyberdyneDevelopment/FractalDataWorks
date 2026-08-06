using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Themes;

/// <summary>
/// Abstract base class for menu themes.
/// Inherit from this class and apply [TypeOption] attribute to create custom themes.
/// </summary>
// Why: pure TypeOption base — constructor only assigns properties, no logic to test.
[ExcludeFromCodeCoverage]
public abstract class MenuThemeBase : TypeOptionBase<int, MenuThemeBase>, IMenuTheme
{
    /// <summary>
    /// Parameterless constructor for the TypeCollection NotFound sentinel.
    /// Not for use by TypeOption implementations.
    /// </summary>
#pragma warning disable CS8618
    protected MenuThemeBase() : base(0, string.Empty) { }
#pragma warning restore CS8618

    /// <summary>
    /// Creates a new menu theme.
    /// </summary>
    protected MenuThemeBase(
        int id,
        string name,
        string themeFamily,
        int colorPaletteId,
        int borderStyleId,
        int iconSetId,
        IColorPalette colors,
        IBorderStyle borders,
        IIconSet icons,
        int paddingSmall = 1,
        int paddingMedium = 2,
        int paddingLarge = 4)
        : base(id, name)
    {
        ThemeFamily = themeFamily;
        ColorPaletteId = colorPaletteId;
        BorderStyleId = borderStyleId;
        IconSetId = iconSetId;
        Colors = colors;
        Borders = borders;
        Icons = icons;
        PaddingSmall = paddingSmall;
        PaddingMedium = paddingMedium;
        PaddingLarge = paddingLarge;
    }

    /// <inheritdoc />
    public string ThemeFamily { get; }

    /// <inheritdoc />
    public int ColorPaletteId { get; }

    /// <inheritdoc />
    public int BorderStyleId { get; }

    /// <inheritdoc />
    public int IconSetId { get; }

    /// <inheritdoc />
    public IColorPalette Colors { get; }

    /// <inheritdoc />
    public IBorderStyle Borders { get; }

    /// <inheritdoc />
    public IIconSet Icons { get; }

    /// <inheritdoc />
    public int PaddingSmall { get; }

    /// <inheritdoc />
    public int PaddingMedium { get; }

    /// <inheritdoc />
    public int PaddingLarge { get; }
}
