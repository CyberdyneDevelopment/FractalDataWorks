using Fdw.Collections;

namespace Fdw.UI.Themes;

/// <summary>
/// Defines a complete theme combining color palette, border styles, and icons.
/// Theme components are referenced by Id for persistence.
/// </summary>
public interface IMenuTheme : ITypeOption<int, MenuThemeBase>
{
    /// <summary>Theme family name (e.g., "Dark", "Light").</summary>
    string ThemeFamily { get; }

    /// <summary>Color palette Id for persistence.</summary>
    int ColorPaletteId { get; }

    /// <summary>Border style Id for persistence.</summary>
    int BorderStyleId { get; }

    /// <summary>Icon set Id for persistence.</summary>
    int IconSetId { get; }

    /// <summary>Resolved color palette.</summary>
    IColorPalette Colors { get; }

    /// <summary>Resolved border style.</summary>
    IBorderStyle Borders { get; }

    /// <summary>Resolved icon set.</summary>
    IIconSet Icons { get; }

    /// <summary>Small padding value (in characters).</summary>
    int PaddingSmall { get; }

    /// <summary>Medium padding value (in characters).</summary>
    int PaddingMedium { get; }

    /// <summary>Large padding value (in characters).</summary>
    int PaddingLarge { get; }
}
