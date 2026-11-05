using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.UI.Themes.Configuration;

/// <summary>
/// Database-backed theme configuration that maps to the <c>settings.Theme</c> table.
/// The source generator automatically registers this type in <c>ConfigurationTypes</c>,
/// and <c>MsSqlConfigurationProvider.Load()</c> queries the table at startup.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Theme")]
public partial class ThemeImplementationConfiguration
    : IThemeImplementationConfiguration
{
    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;



    /// <summary>
    /// Gets or sets the unique identifier for this theme.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the tenant identifier. NULL for system-wide themes available to all tenants.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets when the theme was created (audit field — populated from DB).
    /// </summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>
    /// Gets or sets when the theme was last modified (audit field — populated from DB).
    /// </summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>
    /// Gets or sets the unique name of the theme within a tenant scope.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    // Core Colors

    /// <summary>
    /// Gets or sets the primary brand color.
    /// </summary>
    public string PrimaryColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secondary/accent color.
    /// </summary>
    public string SecondaryColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tertiary color for additional accents.
    /// </summary>
    public string? TertiaryColor { get; set; }

    /// <summary>
    /// Gets or sets the main background color.
    /// </summary>
    public string BackgroundColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the surface/card background color.
    /// </summary>
    public string SurfaceColor { get; set; } = string.Empty;

    // Semantic Colors

    /// <summary>
    /// Gets or sets the error color.
    /// </summary>
    public string ErrorColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the warning color.
    /// </summary>
    public string WarningColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the success color.
    /// </summary>
    public string SuccessColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the info color.
    /// </summary>
    public string InfoColor { get; set; } = string.Empty;

    // Text Colors

    /// <summary>
    /// Gets or sets the primary text color.
    /// </summary>
    public string TextPrimary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secondary/muted text color.
    /// </summary>
    public string TextSecondary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the disabled text color.
    /// </summary>
    public string? TextDisabled { get; set; }

    /// <summary>
    /// Gets or sets the text color on primary-colored surfaces.
    /// </summary>
    public string? TextOnPrimary { get; set; }

    /// <summary>
    /// Gets or sets the text color on secondary-colored surfaces.
    /// </summary>
    public string? TextOnSecondary { get; set; }

    // Typography

    /// <summary>
    /// Gets or sets the primary font family.
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

    // Mode & Branding

    /// <summary>
    /// Gets or sets whether this is a dark mode theme.
    /// </summary>
    public bool IsDarkMode { get; set; }

    /// <summary>
    /// Gets or sets whether this theme is the system default.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets whether this theme is one the system ships, which cannot be removed.
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
    /// Gets or sets the favicon URL.
    /// </summary>
    public string? FaviconUrl { get; set; }

    /// <summary>
    /// Gets or sets the display name for the theme.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the theme description.
    /// </summary>
    public string? Description { get; set; }

}
