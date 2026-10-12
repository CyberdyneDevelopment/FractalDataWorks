using Fdw.UI.Themes;

namespace Fdw.UI.Components.TUI;

/// <summary>
/// Theme configuration for Terminal UI components.
/// </summary>
/// <remarks>
/// Wraps an <see cref="IMenuTheme"/> which provides colors, borders, and icons
/// as TypeCollections. Additional TUI-specific settings can be configured here.
/// </remarks>
public class TUIThemeConfiguration
{
    private IMenuTheme _theme;

    /// <summary>
    /// Initializes a new instance with the default dark theme.
    /// </summary>
    public TUIThemeConfiguration()
    {
        _theme = MenuThemes.ByName("Dark");
    }

    /// <summary>
    /// Initializes a new instance with the specified theme.
    /// </summary>
    /// <param name="theme">The menu theme to use.</param>
    public TUIThemeConfiguration(IMenuTheme theme)
    {
        _theme = theme;
    }

    /// <summary>
    /// Gets or sets the theme by Id.
    /// </summary>
    public int ThemeId
    {
        get => _theme.Id;
        set => _theme = MenuThemes.ById(value);
    }

    /// <summary>
    /// Gets or sets the theme by name.
    /// </summary>
    public string ThemeName
    {
        get => _theme.Name;
        set => _theme = MenuThemes.ByName(value);
    }

    /// <summary>
    /// Gets the current theme.
    /// </summary>
    public IMenuTheme Theme => _theme;

    /// <summary>
    /// Gets the color palette from the current theme.
    /// </summary>
    public IColorPalette Colors => _theme.Colors;

    /// <summary>
    /// Gets the border style from the current theme.
    /// </summary>
    public IBorderStyle Borders => _theme.Borders;

    /// <summary>
    /// Gets the icon set from the current theme.
    /// </summary>
    public IIconSet Icons => _theme.Icons;

    /// <summary>
    /// Gets or sets whether to use color in output.
    /// </summary>
    public bool UseColor { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use emoji/icons in output.
    /// </summary>
    public bool UseEmoji { get; set; } = true;
}
