using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Themes.Clients.Models;

/// <summary>
/// Represents a request to create a new theme.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateThemeRequest
{
    /// <summary>
    /// Gets or sets the unique name of the theme.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the theme.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the theme.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the primary theme color.
    /// </summary>
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// Gets or sets the secondary theme color.
    /// </summary>
    public string? SecondaryColor { get; set; }

    /// <summary>
    /// Gets or sets the tertiary theme color.
    /// </summary>
    public string? TertiaryColor { get; set; }

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the surface color.
    /// </summary>
    public string? SurfaceColor { get; set; }

    /// <summary>
    /// Gets or sets the error semantic color.
    /// </summary>
    public string? ErrorColor { get; set; }

    /// <summary>
    /// Gets or sets the warning semantic color.
    /// </summary>
    public string? WarningColor { get; set; }

    /// <summary>
    /// Gets or sets the success semantic color.
    /// </summary>
    public string? SuccessColor { get; set; }

    /// <summary>
    /// Gets or sets the informational semantic color.
    /// </summary>
    public string? InfoColor { get; set; }

    /// <summary>
    /// Gets or sets the primary text color.
    /// </summary>
    public string? TextPrimary { get; set; }

    /// <summary>
    /// Gets or sets the secondary text color.
    /// </summary>
    public string? TextSecondary { get; set; }

    /// <summary>
    /// Gets or sets the disabled text color.
    /// </summary>
    public string? TextDisabled { get; set; }

    /// <summary>
    /// Gets or sets the text color used on primary-colored backgrounds.
    /// </summary>
    public string? TextOnPrimary { get; set; }

    /// <summary>
    /// Gets or sets the text color used on secondary-colored backgrounds.
    /// </summary>
    public string? TextOnSecondary { get; set; }

    /// <summary>
    /// Gets or sets the font family.
    /// </summary>
    public string? FontFamily { get; set; }

    /// <summary>
    /// Gets or sets the monospace font family.
    /// </summary>
    public string? FontFamilyMono { get; set; }

    /// <summary>
    /// Gets or sets the base font size in pixels.
    /// </summary>
    public int? FontSizeBase { get; set; }

    /// <summary>
    /// Gets or sets the border radius in pixels.
    /// </summary>
    public int? BorderRadius { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether dark mode is enabled.
    /// </summary>
    public bool? IsDarkMode { get; set; }

    /// <summary>
    /// Gets or sets the URL of the logo image.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the application name displayed in the UI.
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// Gets or sets the URL of the favicon.
    /// </summary>
    public string? FaviconUrl { get; set; }

    /// <summary>
    /// Gets or sets additional custom theme properties.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Properties { get; set; }
}
