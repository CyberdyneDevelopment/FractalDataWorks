namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Theme configuration section.
/// </summary>
public sealed class TenantThemeConfiguration
{
    /// <summary>Gets or sets the primary brand color (HSL: 'h s% l%').</summary>
    public string PrimaryColor { get; set; } = "221 83% 53%";
    /// <summary>Gets or sets the secondary brand color (HSL).</summary>
    public string SecondaryColor { get; set; } = "215 16% 47%";
    /// <summary>Gets or sets the accent brand color (HSL).</summary>
    public string AccentColor { get; set; } = "262 83% 58%";

    /// <summary>Gets or sets the background surface color (HSL).</summary>
    public string BackgroundColor { get; set; } = "222 47% 11%";
    /// <summary>Gets or sets the surface color (HSL).</summary>
    public string SurfaceColor { get; set; } = "217 33% 17%";
    /// <summary>Gets or sets the overlay surface color (HSL).</summary>
    public string OverlayColor { get; set; } = "215 28% 23%";

    /// <summary>Gets or sets the success feedback color (HSL).</summary>
    public string SuccessColor { get; set; } = "142 71% 45%";
    /// <summary>Gets or sets the warning feedback color (HSL).</summary>
    public string WarningColor { get; set; } = "38 92% 50%";
    /// <summary>Gets or sets the error feedback color (HSL).</summary>
    public string ErrorColor { get; set; } = "0 84% 60%";
    /// <summary>Gets or sets the info feedback color (HSL).</summary>
    public string InfoColor { get; set; } = "199 89% 48%";

    /// <summary>Gets or sets the main typography color (HSL).</summary>
    public string TextMainColor { get; set; } = "210 40% 98%";
    /// <summary>Gets or sets the muted typography color (HSL).</summary>
    public string TextMutedColor { get; set; } = "215 20% 65%";

    /// <summary>Gets or sets the logo URL.</summary>
    public string? LogoUrl { get; set; }
    /// <summary>Gets or sets the favicon URL.</summary>
    public string? FaviconUrl { get; set; }
    /// <summary>Gets or sets the custom CSS URL.</summary>
    public string? CustomCssUrl { get; set; }
    /// <summary>Gets or sets whether dark mode is default.</summary>
    public bool DarkModeDefault { get; set; } = true;

    /// <summary>
    /// Converts to ITenantTheme.
    /// </summary>
    public ITenantTheme ToTheme() => new TenantTheme
    {
        PrimaryColor = PrimaryColor,
        SecondaryColor = SecondaryColor,
        AccentColor = AccentColor,
        BackgroundColor = BackgroundColor,
        SurfaceColor = SurfaceColor,
        OverlayColor = OverlayColor,
        SuccessColor = SuccessColor,
        WarningColor = WarningColor,
        ErrorColor = ErrorColor,
        InfoColor = InfoColor,
        TextMainColor = TextMainColor,
        TextMutedColor = TextMutedColor,
        LogoUrl = LogoUrl,
        FaviconUrl = FaviconUrl,
        CustomCssUrl = CustomCssUrl,
        DarkModeDefault = DarkModeDefault
    };
}
