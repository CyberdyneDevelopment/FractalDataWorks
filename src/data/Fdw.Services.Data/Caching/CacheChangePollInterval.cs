using System;
using Fdw.Results;
using Fdw.Services.Data.Logging;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Caching;

/// <summary>
/// Reads how often a host with a result cache looks at the configuration changes the other hosts published (FDW-4782).
/// </summary>
/// <remarks>
/// The number is read for the host, with no tenant. A number the setting
/// <see cref="SettingDefinitions.CacheChangePollIntervalSeconds"/> states wins, and one that is not positive fails and names the
/// setting. When it states none, or the host registers no settings, the interval the host declared at registration applies
/// (<see cref="ConfigurationGatewayTypes.CacheChangePollInterval"/>).
/// </remarks>
public static class CacheChangePollInterval
{
    /// <summary>Reads the interval.</summary>
    /// <param name="settings">The settings a stated number is read from, or null when the host registers none.</param>
    /// <param name="declared">The interval the host declared at registration.</param>
    /// <param name="logger">The logger a failure is written to.</param>
    /// <returns>The interval, or the failure naming the setting.</returns>
    public static IGenericResult<TimeSpan> Read(IEffectiveSettingsProvider? settings, TimeSpan declared, ILogger logger)
    {
        if (settings is null || !settings.TryGetEffectiveValue<int>(SettingDefinitions.CacheChangePollIntervalSeconds, out var seconds))
            return GenericResult<TimeSpan>.Success(declared);

        return seconds <= 0
            ? GenericResult<TimeSpan>.Failure(CacheChangeLog.IntervalSettingNotPositive(logger, SettingDefinitions.CacheChangePollIntervalSeconds, seconds))
            : GenericResult<TimeSpan>.Success(TimeSpan.FromSeconds(seconds));
    }
}
