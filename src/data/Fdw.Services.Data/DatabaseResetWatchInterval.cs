using System;
using Fdw.Results;
using Fdw.Services.Data.Logging;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// Reads how often a host looks at its configuration databases to see whether one was reset (FDW-3207).
/// </summary>
/// <remarks>
/// The number is read for the host, with no tenant. A number that is not set to a positive value fails and names the setting; no number
/// is assumed.
/// </remarks>
public static class DatabaseResetWatchInterval
{
    /// <summary>Reads the interval from the platform's settings.</summary>
    /// <param name="settings">The settings the number is resolved from.</param>
    /// <param name="logger">The logger a failure is written to.</param>
    /// <returns>The interval, or the failure naming the setting.</returns>
    public static IGenericResult<TimeSpan> Read(IEffectiveSettingsProvider settings, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        var seconds = settings.GetEffectiveValue<int>(SettingDefinitions.ResetWatchIntervalSeconds);
        return seconds <= 0
            ? GenericResult<TimeSpan>.Failure(DatabaseResetWatcherLog.IntervalSettingMissing(logger, SettingDefinitions.ResetWatchIntervalSeconds))
            : GenericResult<TimeSpan>.Success(TimeSpan.FromSeconds(seconds));
    }
}
