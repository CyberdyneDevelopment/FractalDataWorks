using Fdw.UI.Themes.Clients.Models;

namespace Fdw.UI.Themes.Configuration;

/// <summary>
/// Extension methods for mapping between <see cref="ThemeManagedConfiguration"/> (database)
/// and <see cref="ThemeConfiguration"/> (API DTO).
/// </summary>
public static class ThemeConfigurationMapper
{
    /// <summary>
    /// Converts a database-backed <see cref="ThemeManagedConfiguration"/> to a
    /// <see cref="ThemeConfiguration"/> DTO for API transport.
    /// </summary>
    public static ThemeConfiguration ToDto(this ThemeManagedConfiguration managed)
    {
        return new ThemeConfiguration
        {
            Id = managed.Id,
            TenantId = managed.TenantId,
            Name = managed.Name,
            DisplayName = managed.DisplayName,
            Description = managed.Description,
            PrimaryColor = managed.PrimaryColor,
            SecondaryColor = managed.SecondaryColor,
            TertiaryColor = managed.TertiaryColor ?? string.Empty,
            BackgroundColor = managed.BackgroundColor,
            SurfaceColor = managed.SurfaceColor,
            ErrorColor = managed.ErrorColor,
            WarningColor = managed.WarningColor,
            SuccessColor = managed.SuccessColor,
            InfoColor = managed.InfoColor,
            TextPrimary = managed.TextPrimary,
            TextSecondary = managed.TextSecondary,
            TextDisabled = managed.TextDisabled ?? string.Empty,
            TextOnPrimary = managed.TextOnPrimary ?? "#FFFFFF",
            TextOnSecondary = managed.TextOnSecondary ?? "#FFFFFF",
            FontFamily = managed.FontFamily,
            FontFamilyMono = managed.FontFamilyMono,
            FontSizeBase = managed.FontSizeBase,
            BorderRadius = managed.BorderRadius,
            IsDarkMode = managed.IsDarkMode,
            IsDefault = managed.IsDefault,
            LogoUrl = managed.LogoUrl,
            AppName = managed.AppName,
            FaviconUrl = managed.FaviconUrl,
            CreatedAt = managed.CreateDate
        };
    }

    /// <summary>
    /// Converts a <see cref="ThemeConfiguration"/> DTO to a
    /// <see cref="ThemeManagedConfiguration"/> for database persistence.
    /// </summary>
    public static ThemeManagedConfiguration ToManaged(this ThemeConfiguration dto)
    {
        return new ThemeManagedConfiguration
        {
            Id = dto.Id,
            TenantId = dto.TenantId,
            Name = dto.Name,
            DisplayName = dto.DisplayName,
            Description = dto.Description,
            PrimaryColor = dto.PrimaryColor,
            SecondaryColor = dto.SecondaryColor,
            TertiaryColor = dto.TertiaryColor,
            BackgroundColor = dto.BackgroundColor,
            SurfaceColor = dto.SurfaceColor,
            ErrorColor = dto.ErrorColor,
            WarningColor = dto.WarningColor,
            SuccessColor = dto.SuccessColor,
            InfoColor = dto.InfoColor,
            TextPrimary = dto.TextPrimary,
            TextSecondary = dto.TextSecondary,
            TextDisabled = dto.TextDisabled,
            TextOnPrimary = dto.TextOnPrimary,
            TextOnSecondary = dto.TextOnSecondary,
            FontFamily = dto.FontFamily,
            FontFamilyMono = dto.FontFamilyMono,
            FontSizeBase = dto.FontSizeBase,
            BorderRadius = dto.BorderRadius,
            IsDarkMode = dto.IsDarkMode,
            IsDefault = dto.IsDefault,
            LogoUrl = dto.LogoUrl,
            AppName = dto.AppName,
            FaviconUrl = dto.FaviconUrl
        };
    }

    /// <summary>
    /// Converts a <see cref="ThemeManagedConfiguration"/> to a <see cref="ThemeSummaryPayload"/>.
    /// </summary>
    public static ThemeSummaryPayload ToSummary(this ThemeManagedConfiguration managed)
    {
        return new ThemeSummaryPayload
        {
            Id = managed.Id,
            TenantId = managed.TenantId,
            Name = managed.Name,
            DisplayName = managed.DisplayName,
            Description = managed.Description,
            PrimaryColor = managed.PrimaryColor,
            SecondaryColor = managed.SecondaryColor,
            BackgroundColor = managed.BackgroundColor,
            IsDarkMode = managed.IsDarkMode,
            IsDefault = managed.IsDefault
        };
    }
}
