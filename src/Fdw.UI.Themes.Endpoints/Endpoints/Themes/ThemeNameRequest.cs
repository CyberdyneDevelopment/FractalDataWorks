using System;

namespace Fdw.UI.Themes.Endpoints;

/// <summary>
/// Request to retrieve a theme by name.
/// </summary>
public class ThemeNameRequest
{
    /// <summary>Gets or sets the theme name (bound from route).</summary>
    public string Name { get; set; } = string.Empty;
}