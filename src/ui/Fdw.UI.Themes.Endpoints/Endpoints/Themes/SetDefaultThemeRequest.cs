namespace Fdw.UI.Themes.Endpoints;

/// <summary>
/// Request to set a theme as the system default.
/// </summary>
public class SetDefaultThemeRequest
{
    /// <summary>Gets or sets the theme name to set as default (bound from route).</summary>
    public string Name { get; set; } = string.Empty;
}