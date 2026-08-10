namespace Fdw.UI.Themes.Endpoints;

/// <summary>
/// Request to delete a theme by name.
/// </summary>
public class DeleteThemeRequest
{
    /// <summary>Gets or sets the theme name to delete (bound from route).</summary>
    public string Name { get; set; } = string.Empty;
}