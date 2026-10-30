namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Request model for updating user preferences.
/// </summary>
public class UpdateUserPreferencesRequest
{
    /// <summary>
    /// Gets or sets the theme name.
    /// </summary>
    public string? ThemeName { get; set; }

    /// <summary>
    /// Gets or sets whether dark mode is enabled: true chooses the dark colour mode and false the light one. Kept for callers that predate
    /// <see cref="ColorMode"/>; when both are stated they must agree.
    /// </summary>
    public bool? DarkMode { get; set; }

    /// <summary>
    /// Gets or sets the colour mode to choose: "light", "dark" or "system".
    /// </summary>
    public string? ColorMode { get; set; }

    /// <summary>
    /// Gets or sets the language preference.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the timezone preference.
    /// </summary>
    public string? Timezone { get; set; }
}
