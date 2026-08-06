using System;

namespace Fdw.UI.Themes.Clients.Models;

/// <summary>
/// Represents a complete theme configuration with colors, typography, and branding settings.
/// </summary>
public sealed class ThemeConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier of the theme.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the tenant identifier. NULL for system-wide themes.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets when the theme was created (audit field).
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the unique name of the theme within a tenant scope.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the theme.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description of the theme.
    /// </summary>
    public string? Description { get; set; }

    // Core Colors

    /// <summary>
    /// Gets or sets the primary theme color.
    /// </summary>
    public string PrimaryColor { get; set; } = "#1976D2";

    /// <summary>
    /// Gets or sets the secondary theme color.
    /// </summary>
    public string SecondaryColor { get; set; } = "#424242";

    /// <summary>
    /// Gets or sets the tertiary theme color.
    /// </summary>
    public string TertiaryColor { get; set; } = "#7B1FA2";

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public string BackgroundColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// Gets or sets the surface color.
    /// </summary>
    public string SurfaceColor { get; set; } = "#F5F5F5";

    // Semantic Colors

    /// <summary>
    /// Gets or sets the error semantic color.
    /// </summary>
    public string ErrorColor { get; set; } = "#D32F2F";

    /// <summary>
    /// Gets or sets the warning semantic color.
    /// </summary>
    public string WarningColor { get; set; } = "#FFA000";

    /// <summary>
    /// Gets or sets the success semantic color.
    /// </summary>
    public string SuccessColor { get; set; } = "#388E3C";

    /// <summary>
    /// Gets or sets the informational semantic color.
    /// </summary>
    public string InfoColor { get; set; } = "#1976D2";

    // Text Colors

    /// <summary>
    /// Gets or sets the primary text color.
    /// </summary>
    public string TextPrimary { get; set; } = "#212121";

    /// <summary>
    /// Gets or sets the secondary text color.
    /// </summary>
    public string TextSecondary { get; set; } = "#757575";

    /// <summary>
    /// Gets or sets the disabled text color.
    /// </summary>
    public string TextDisabled { get; set; } = "#9E9E9E";

    /// <summary>
    /// Gets or sets the text color used on primary-colored backgrounds.
    /// </summary>
    public string TextOnPrimary { get; set; } = "#FFFFFF";

    /// <summary>
    /// Gets or sets the text color used on secondary-colored backgrounds.
    /// </summary>
    public string TextOnSecondary { get; set; } = "#FFFFFF";

    // Typography

    /// <summary>
    /// Gets or sets the font family.
    /// </summary>
    public string FontFamily { get; set; } = "Roboto, sans-serif";

    /// <summary>
    /// Gets or sets the monospace font family.
    /// </summary>
    public string FontFamilyMono { get; set; } = "JetBrains Mono, Consolas, monospace";

    /// <summary>
    /// Gets or sets the base font size in pixels.
    /// </summary>
    public int FontSizeBase { get; set; } = 14;

    /// <summary>
    /// Gets or sets the border radius in pixels.
    /// </summary>
    public int BorderRadius { get; set; } = 4;

    // Mode and Branding

    /// <summary>
    /// Gets or sets a value indicating whether dark mode is enabled.
    /// </summary>
    public bool IsDarkMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the default theme.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets the URL of the logo image.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the application name displayed in the UI.
    /// </summary>
    public string AppName { get; set; } = "Fdw";

    /// <summary>
    /// Gets or sets the URL of the favicon.
    /// </summary>
    public string? FaviconUrl { get; set; }

    /// <summary>
    /// Creates a deep copy of this theme configuration.
    /// </summary>
    /// <returns>A new <see cref="ThemeConfiguration"/> instance with the same property values.</returns>
    public ThemeConfiguration Clone() => new()
    {
        Id = Id, TenantId = TenantId, Name = Name, DisplayName = DisplayName, Description = Description,
        PrimaryColor = PrimaryColor, SecondaryColor = SecondaryColor, TertiaryColor = TertiaryColor,
        BackgroundColor = BackgroundColor, SurfaceColor = SurfaceColor,
        ErrorColor = ErrorColor, WarningColor = WarningColor, SuccessColor = SuccessColor, InfoColor = InfoColor,
        TextPrimary = TextPrimary, TextSecondary = TextSecondary, TextDisabled = TextDisabled,
        TextOnPrimary = TextOnPrimary, TextOnSecondary = TextOnSecondary,
        FontFamily = FontFamily, FontFamilyMono = FontFamilyMono, FontSizeBase = FontSizeBase, BorderRadius = BorderRadius,
        IsDarkMode = IsDarkMode, IsDefault = IsDefault, LogoUrl = LogoUrl, AppName = AppName, FaviconUrl = FaviconUrl
    };

    /// <summary>
    /// Creates a default light theme configuration.
    /// </summary>
    /// <returns>A new <see cref="ThemeConfiguration"/> with default light theme settings.</returns>
    public static ThemeConfiguration CreateDefaultLight() => new()
    {
        Name = "default-light", DisplayName = "Default Light", Description = "Standard light theme", IsDefault = true
    };

    /// <summary>
    /// Creates a default dark theme configuration.
    /// </summary>
    /// <returns>A new <see cref="ThemeConfiguration"/> with default dark theme settings.</returns>
    public static ThemeConfiguration CreateDefaultDark() => new()
    {
        Name = "default-dark", DisplayName = "Default Dark", Description = "Standard dark theme", IsDarkMode = true,
        PrimaryColor = "#90CAF9", SecondaryColor = "#CE93D8", TertiaryColor = "#FFB74D",
        BackgroundColor = "#121212", SurfaceColor = "#1E1E1E",
        ErrorColor = "#EF5350", WarningColor = "#FFB74D", SuccessColor = "#66BB6A", InfoColor = "#42A5F5",
        TextPrimary = "#E0E0E0", TextSecondary = "#9E9E9E", TextDisabled = "#616161",
        TextOnPrimary = "#000000", TextOnSecondary = "#000000"
    };

    /// <summary>
    /// Creates the Fdw brand theme with deep logic purple colors.
    /// </summary>
    /// <returns>A new <see cref="ThemeConfiguration"/> with Fdw brand theme settings.</returns>
    public static ThemeConfiguration CreateFractalTheme() => new()
    {
        Name = "fractal", DisplayName = "Fractal", Description = "Deep Logic Purple - Fdw brand theme", IsDarkMode = true,
        PrimaryColor = "#7209B7", SecondaryColor = "#3A0CA3", TertiaryColor = "#F72585",
        BackgroundColor = "#0F1115", SurfaceColor = "#1A1D24",
        ErrorColor = "#FF6B6B", WarningColor = "#FFE66D", SuccessColor = "#4ECDC4", InfoColor = "#4EA8DE",
        TextPrimary = "#E2E8F0", TextSecondary = "#94A3B8", TextDisabled = "#64748B",
        TextOnPrimary = "#FFFFFF", TextOnSecondary = "#FFFFFF"
    };
}
