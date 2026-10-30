using System;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Users.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Judges what a preferences update states, and applies it to the stored preferences only when all of it is acceptable (FDW-3061).
/// </summary>
/// <remarks>
/// Why one place: the update endpoint stays an orchestrator, and the rules for a colour mode, a time zone and the legacy
/// <c>darkMode</c> flag are written once where a test can reach them. Nothing is stored when any part of the update is refused.
/// </remarks>
public static class UserPreferenceChanges
{
    /// <summary>Checks an update and, when it is acceptable, applies it to the preferences.</summary>
    /// <param name="request">What the person stated; a field that is null is left as it is.</param>
    /// <param name="preferences">The stored preferences to change.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>Success, or the refusal that names the value and says what is accepted.</returns>
    public static IGenericResult Apply(
        UpdateUserPreferencesRequest request, IUserPreferencesImplementationConfiguration preferences, ILogger logger)
    {
        var colorMode = ColorModeStated(request, logger, out var chosen);
        if (colorMode.IsFailure) return colorMode;

        var zone = TimeZoneStated(request.Timezone, logger);
        if (zone.IsFailure) return zone;

        if (request.ThemeName is not null) preferences.ThemeName = request.ThemeName;
        if (chosen is not null)
        {
            preferences.ColorMode = chosen;
            preferences.DarkMode = string.Equals(chosen, UserColorModes.Dark, StringComparison.Ordinal);
        }

        if (request.Language is not null) preferences.Language = request.Language;
        if (request.Timezone is not null) preferences.Timezone = request.Timezone;
        return GenericResult.Success();
    }

    // The mode the update chooses: its colorMode, or the one its legacy darkMode flag means. Both stated must agree.
    private static IGenericResult ColorModeStated(UpdateUserPreferencesRequest request, ILogger logger, out string? chosen)
    {
        chosen = null;
        if (request.ColorMode is null)
        {
            chosen = request.DarkMode switch
            {
                true => UserColorModes.Dark,
                false => UserColorModes.Light,
                null => null,
            };
            return GenericResult.Success();
        }

        if (!UserColorModes.All.Contains(request.ColorMode, StringComparer.Ordinal))
        {
            return GenericResult.Failure(UserEndpointLog.ColorModeNotOffered(logger, request.ColorMode));
        }

        var isDark = string.Equals(request.ColorMode, UserColorModes.Dark, StringComparison.Ordinal);
        if (request.DarkMode is { } darkMode && darkMode != isDark)
        {
            return GenericResult.Failure(UserEndpointLog.ColorModeAndDarkModeDisagree(logger, request.ColorMode, darkMode));
        }

        chosen = request.ColorMode;
        return GenericResult.Success();
    }

    // A time zone is good exactly when the server can resolve it, which is what everything that later reads it does.
    private static IGenericResult TimeZoneStated(string? timeZoneId, ILogger logger)
    {
        if (timeZoneId is null) return GenericResult.Success();
        if (string.IsNullOrWhiteSpace(timeZoneId)) return GenericResult.Failure(UserEndpointLog.TimeZoneEmpty(logger));

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return GenericResult.Success();
        }
        catch (TimeZoneNotFoundException)
        {
            return GenericResult.Failure(UserEndpointLog.TimeZoneNotKnown(logger, timeZoneId));
        }
        catch (InvalidTimeZoneException)
        {
            return GenericResult.Failure(UserEndpointLog.TimeZoneNotKnown(logger, timeZoneId));
        }
    }
}
