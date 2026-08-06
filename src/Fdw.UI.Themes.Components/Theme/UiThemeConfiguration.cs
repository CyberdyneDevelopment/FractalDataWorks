using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Themes.Components.Theme;

/// <summary>
/// Theme configuration for UI components.
/// Cascades through component tree via CascadingValue.
/// </summary>
// Why: pure theme descriptor — properties + literal preset values, no logic.
[ExcludeFromCodeCoverage]
public class UiThemeConfiguration
{
    /// <summary>
    /// Gets or sets the color scheme for the theme.
    /// </summary>
    public UiColorScheme Colors { get; set; } = UiColorScheme.Dark;

    /// <summary>
    /// Gets or sets the font family for the theme.
    /// </summary>
    public string FontFamily { get; set; } = "Inter, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif";

    /// <summary>
    /// Gets or sets the base font size for the theme.
    /// </summary>
    public string FontSize { get; set; } = "14px";

    /// <summary>
    /// Gets or sets the line height for the theme.
    /// </summary>
    public string LineHeight { get; set; } = "1.5";

    /// <summary>
    /// Gets or sets the border radius for the theme.
    /// </summary>
    public string BorderRadius { get; set; } = "4px";

    /// <summary>
    /// Gets or sets the spacing unit for the theme.
    /// </summary>
    public string SpacingUnit { get; set; } = "8px";

    /// <summary>
    /// Gets or sets custom properties for the theme.
    /// </summary>
    public IDictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>(System.StringComparer.Ordinal);

    /// <summary>
    /// Creates a dark theme configuration.
    /// </summary>
    public static UiThemeConfiguration DarkTheme => new() { Colors = UiColorScheme.Dark };

    /// <summary>
    /// Creates a light theme configuration.
    /// </summary>
    public static UiThemeConfiguration LightTheme => new() { Colors = UiColorScheme.Light };
}