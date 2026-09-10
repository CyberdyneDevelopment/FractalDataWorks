using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Themes.Configuration;

/// <summary>The contract every Theme implementation carries.</summary>
public interface IThemeImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record\'s durable id.</summary>
    Guid ThemeId { get; set; }

    /// <summary>Gets or sets the Theme TenantId.</summary>
    Guid? TenantId { get; set; }

    /// <summary>Gets or sets the Theme CreateDate.</summary>
    DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the Theme ModifyDate.</summary>
    DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the Theme PrimaryColor.</summary>
    string PrimaryColor { get; set; }

    /// <summary>Gets or sets the Theme SecondaryColor.</summary>
    string SecondaryColor { get; set; }

    /// <summary>Gets or sets the Theme TertiaryColor.</summary>
    string? TertiaryColor { get; set; }

    /// <summary>Gets or sets the Theme BackgroundColor.</summary>
    string BackgroundColor { get; set; }

    /// <summary>Gets or sets the Theme SurfaceColor.</summary>
    string SurfaceColor { get; set; }

    /// <summary>Gets or sets the Theme ErrorColor.</summary>
    string ErrorColor { get; set; }

    /// <summary>Gets or sets the Theme WarningColor.</summary>
    string WarningColor { get; set; }

    /// <summary>Gets or sets the Theme SuccessColor.</summary>
    string SuccessColor { get; set; }

    /// <summary>Gets or sets the Theme InfoColor.</summary>
    string InfoColor { get; set; }

    /// <summary>Gets or sets the Theme TextPrimary.</summary>
    string TextPrimary { get; set; }

    /// <summary>Gets or sets the Theme TextSecondary.</summary>
    string TextSecondary { get; set; }

    /// <summary>Gets or sets the Theme TextDisabled.</summary>
    string? TextDisabled { get; set; }

    /// <summary>Gets or sets the Theme TextOnPrimary.</summary>
    string? TextOnPrimary { get; set; }

    /// <summary>Gets or sets the Theme TextOnSecondary.</summary>
    string? TextOnSecondary { get; set; }

    /// <summary>Gets or sets the Theme FontFamily.</summary>
    string FontFamily { get; set; }

    /// <summary>Gets or sets the Theme FontFamilyMono.</summary>
    string FontFamilyMono { get; set; }

    /// <summary>Gets or sets the Theme FontSizeBase.</summary>
    int FontSizeBase { get; set; }

    /// <summary>Gets or sets the Theme BorderRadius.</summary>
    int BorderRadius { get; set; }

    /// <summary>Gets or sets the Theme IsDarkMode.</summary>
    bool IsDarkMode { get; set; }

    /// <summary>Gets or sets the Theme IsDefault.</summary>
    bool IsDefault { get; set; }

    /// <summary>Gets or sets the Theme LogoUrl.</summary>
    string? LogoUrl { get; set; }

    /// <summary>Gets or sets the Theme AppName.</summary>
    string AppName { get; set; }

    /// <summary>Gets or sets the Theme FaviconUrl.</summary>
    string? FaviconUrl { get; set; }

    /// <summary>Gets or sets the Theme DisplayName.</summary>
    string? DisplayName { get; set; }

    /// <summary>Gets or sets the Theme Description.</summary>
    string? Description { get; set; }
}
