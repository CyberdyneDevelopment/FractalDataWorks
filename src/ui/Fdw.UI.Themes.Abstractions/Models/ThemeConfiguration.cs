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
    public Guid Id { get; set; }

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
    public string PrimaryColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secondary theme color.
    /// </summary>
    public string SecondaryColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tertiary theme color.
    /// </summary>
    public string TertiaryColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public string BackgroundColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the surface color.
    /// </summary>
    public string SurfaceColor { get; set; } = string.Empty;

    // Semantic Colors

    /// <summary>
    /// Gets or sets the error semantic color.
    /// </summary>
    public string ErrorColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the warning semantic color.
    /// </summary>
    public string WarningColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the success semantic color.
    /// </summary>
    public string SuccessColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the informational semantic color.
    /// </summary>
    public string InfoColor { get; set; } = string.Empty;

    // Text Colors

    /// <summary>
    /// Gets or sets the primary text color.
    /// </summary>
    public string TextPrimary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secondary text color.
    /// </summary>
    public string TextSecondary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the disabled text color.
    /// </summary>
    public string TextDisabled { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the text color used on primary-colored backgrounds.
    /// </summary>
    public string TextOnPrimary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the text color used on secondary-colored backgrounds.
    /// </summary>
    public string TextOnSecondary { get; set; } = string.Empty;

    // Typography

    /// <summary>
    /// Gets or sets the font family.
    /// </summary>
    public string FontFamily { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the monospace font family.
    /// </summary>
    public string FontFamilyMono { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base font size in pixels.
    /// </summary>
    public int FontSizeBase { get; set; }

    /// <summary>
    /// Gets or sets the border radius in pixels.
    /// </summary>
    public int BorderRadius { get; set; }

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
    /// Gets or sets a value indicating whether this is a built-in theme, which cannot be removed.
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// Gets or sets the URL of the logo image.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the application name displayed in the UI.
    /// </summary>
    public string AppName { get; set; } = string.Empty;

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
        IsDarkMode = IsDarkMode, IsDefault = IsDefault, IsBuiltIn = IsBuiltIn, LogoUrl = LogoUrl, AppName = AppName, FaviconUrl = FaviconUrl
    };
}
