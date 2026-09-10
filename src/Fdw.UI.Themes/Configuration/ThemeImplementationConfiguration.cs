using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.UI.Themes.Configuration;

/// <summary>Everything a configured theme carries: its palette, typography and branding.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class ThemeImplementationConfiguration
    : IThemeImplementationConfiguration
{

    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <summary>The domain record's durable id.</summary>
    public Guid ThemeId { get; set; }

    /// <summary>The domain record's row id -- the foreign key the constraint is on.</summary>
    public int ThemeRowId { get; set; }

    /// <summary>Gets or sets the theme's DisplayName.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the theme's Description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the theme's PrimaryColor.</summary>
    public string PrimaryColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the theme's SecondaryColor.</summary>
    public string SecondaryColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the theme's TertiaryColor.</summary>
    public string? TertiaryColor { get; set; }

    /// <summary>Gets or sets the theme's BackgroundColor.</summary>
    public string BackgroundColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the theme's SurfaceColor.</summary>
    public string SurfaceColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the theme's ErrorColor.</summary>
    public string ErrorColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the theme's WarningColor.</summary>
    public string WarningColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the theme's SuccessColor.</summary>
    public string SuccessColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the theme's InfoColor.</summary>
    public string InfoColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the theme's TextPrimary.</summary>
    public string TextPrimary { get; set; } = string.Empty;

    /// <summary>Gets or sets the theme's TextSecondary.</summary>
    public string TextSecondary { get; set; } = string.Empty;

    /// <summary>Gets or sets the theme's TextDisabled.</summary>
    public string? TextDisabled { get; set; }

    /// <summary>Gets or sets the theme's TextOnPrimary.</summary>
    public string? TextOnPrimary { get; set; }

    /// <summary>Gets or sets the theme's TextOnSecondary.</summary>
    public string? TextOnSecondary { get; set; }

    /// <summary>Gets or sets the theme's FontFamily.</summary>
    public string FontFamily { get; set; } = string.Empty;

    /// <summary>Gets or sets the theme's FontFamilyMono.</summary>
    public string FontFamilyMono { get; set; } = string.Empty;

    /// <summary>Gets or sets the theme's FontSizeBase.</summary>
    public int FontSizeBase { get; set; }

    /// <summary>Gets or sets the theme's BorderRadius.</summary>
    public int BorderRadius { get; set; }

    /// <summary>Gets or sets the theme's IsDarkMode.</summary>
    public bool IsDarkMode { get; set; }

    /// <summary>Gets or sets the theme's IsDefault.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Gets or sets the theme's LogoUrl.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Gets or sets the theme's AppName.</summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>Gets or sets the theme's FaviconUrl.</summary>
    public string? FaviconUrl { get; set; }
}
