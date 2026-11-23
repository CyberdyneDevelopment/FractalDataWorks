using Fdw.Results;
using Fdw.UI.Themes.Clients.Models;
using Fdw.UI.Themes.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.UI.Themes.Configuration;

/// <summary>
/// Extension methods for mapping between <see cref="ThemeImplementationConfiguration"/> (database)
/// and <see cref="ThemeConfiguration"/> (API DTO).
/// </summary>
public static class ThemeConfigurationMapper
{
    /// <summary>
    /// Converts a database-backed <see cref="ThemeImplementationConfiguration"/> to a
    /// <see cref="ThemeConfiguration"/> DTO for API transport.
    /// </summary>
    /// <param name="managed">The stored theme.</param>
    /// <param name="logger">The logger that records a refusal.</param>
    /// <returns>
    /// The DTO; a failure naming the theme and the text color when the theme states no <c>TextOnPrimary</c> or
    /// <c>TextOnSecondary</c> (<c>THEMES-63100</c>). A theme row's text colors are stated, never supplied here (FDW-6479).
    /// </returns>
    public static IGenericResult<ThemeConfiguration> ToDto(this IThemeImplementationConfiguration managed, ILogger logger)
    {
        var log = logger ?? NullLogger.Instance;

        if (managed.TextOnPrimary is not { } textOnPrimary)
        {
            return GenericResult<ThemeConfiguration>.Failure(
                ThemeConfigurationProviderLog.ThemeStatesNoTextColor(log, managed.Name, nameof(managed.TextOnPrimary)));
        }

        if (managed.TextOnSecondary is not { } textOnSecondary)
        {
            return GenericResult<ThemeConfiguration>.Failure(
                ThemeConfigurationProviderLog.ThemeStatesNoTextColor(log, managed.Name, nameof(managed.TextOnSecondary)));
        }

        return GenericResult<ThemeConfiguration>.Success(new ThemeConfiguration
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
            TextOnPrimary = textOnPrimary,
            TextOnSecondary = textOnSecondary,
            FontFamily = managed.FontFamily,
            FontFamilyMono = managed.FontFamilyMono,
            FontSizeBase = managed.FontSizeBase,
            BorderRadius = managed.BorderRadius,
            IsDarkMode = managed.IsDarkMode,
            IsDefault = managed.IsDefault,
            IsBuiltIn = managed.IsBuiltIn,
            LogoUrl = managed.LogoUrl,
            AppName = managed.AppName,
            FaviconUrl = managed.FaviconUrl,
            CreatedAt = managed.CreateDate
        });
    }

    /// <summary>
    /// Converts a <see cref="ThemeConfiguration"/> DTO to a
    /// <see cref="ThemeImplementationConfiguration"/> for database persistence.
    /// </summary>
    public static IThemeImplementationConfiguration ToManaged(this ThemeConfiguration dto)
    {
        return new ThemeImplementationConfiguration
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
            IsBuiltIn = dto.IsBuiltIn,
            LogoUrl = dto.LogoUrl,
            AppName = dto.AppName,
            FaviconUrl = dto.FaviconUrl
        };
    }

    /// <summary>
    /// Converts a <see cref="ThemeImplementationConfiguration"/> to a <see cref="ThemeSummaryPayload"/>.
    /// </summary>
    public static ThemeSummaryPayload ToSummary(this IThemeImplementationConfiguration managed)
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
            IsDefault = managed.IsDefault,
            IsBuiltIn = managed.IsBuiltIn
        };
    }
}
