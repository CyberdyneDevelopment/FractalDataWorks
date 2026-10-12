namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Data transfer object for tenant theme settings.
/// </summary>
public sealed class TenantThemeDto
{
    /// <summary>Gets or sets the primary brand color (HSL).</summary>
    public string PrimaryColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the secondary brand color (HSL).</summary>
    public string SecondaryColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the accent brand color (HSL).</summary>
    public string AccentColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the background surface color (HSL).</summary>
    public string BackgroundColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the surface color (HSL).</summary>
    public string SurfaceColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the overlay surface color (HSL).</summary>
    public string OverlayColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the success feedback color (HSL).</summary>
    public string SuccessColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the warning feedback color (HSL).</summary>
    public string WarningColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the error feedback color (HSL).</summary>
    public string ErrorColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the info feedback color (HSL).</summary>
    public string InfoColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the main typography color (HSL).</summary>
    public string TextMainColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the muted typography color (HSL).</summary>
    public string TextMutedColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the logo URL.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Gets or sets the favicon URL.</summary>
    public string? FaviconUrl { get; set; }

    /// <summary>Gets or sets the custom CSS URL.</summary>
    public string? CustomCssUrl { get; set; }

    /// <summary>Gets or sets whether dark mode is the default.</summary>
    public bool DarkModeDefault { get; set; }

    /// <summary>
    /// Creates a DTO from an ITenantTheme.
    /// </summary>
    public static TenantThemeDto FromTheme(ITenantTheme theme)
    {
        return new TenantThemeDto
        {
            PrimaryColor = theme.PrimaryColor,
            SecondaryColor = theme.SecondaryColor,
            AccentColor = theme.AccentColor,
            BackgroundColor = theme.BackgroundColor,
            SurfaceColor = theme.SurfaceColor,
            OverlayColor = theme.OverlayColor,
            SuccessColor = theme.SuccessColor,
            WarningColor = theme.WarningColor,
            ErrorColor = theme.ErrorColor,
            InfoColor = theme.InfoColor,
            TextMainColor = theme.TextMainColor,
            TextMutedColor = theme.TextMutedColor,
            LogoUrl = theme.LogoUrl,
            FaviconUrl = theme.FaviconUrl,
            CustomCssUrl = theme.CustomCssUrl,
            DarkModeDefault = theme.DarkModeDefault
        };
    }
}
