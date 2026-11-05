using System;
using Fdw.Results;
using Fdw.Services.Settings.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings;

/// <summary>
/// Gives a connection its connect timeout (FDW-3580): the number it states, else the instance's setting
/// <see cref="SettingDefinitions.DefaultConnectionTimeoutSeconds"/>. No number is assumed in code or in a column, so one that has neither is a
/// failure that names it and the setting.
/// </summary>
public static class InstanceConnectionTimeout
{
    private static readonly TimeoutMessages Messages = new(
        ConnectionTimeoutLog.NotPositive, ConnectionTimeoutLog.NoSettings, ConnectionTimeoutLog.ReadThroughItself, ConnectionTimeoutLog.NotStated);

    /// <summary>Resolves the connect timeout.</summary>
    /// <param name="stated">The connect timeout the connection states, or <see langword="null"/> when it states none.</param>
    /// <param name="settings">The host's settings, read when the connection states none; <see langword="null"/> in a host that registers no settings.</param>
    /// <param name="ownerKind">What is being built, as a person says it: "connection".</param>
    /// <param name="ownerName">The name of the connection.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The connect timeout in seconds, positive; a failure naming the connection, and the setting when it is the one missing.</returns>
    public static IGenericResult<int> Resolve(int? stated, Lazy<IEffectiveSettingsProvider>? settings, string ownerKind, string ownerName, ILogger logger)
        => InstanceTimeout.Resolve(stated, settings, SettingDefinitions.DefaultConnectionTimeoutSeconds, ownerKind, ownerName, logger, Messages);
}
