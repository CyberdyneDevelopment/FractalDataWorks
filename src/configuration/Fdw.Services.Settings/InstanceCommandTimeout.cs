using System;
using Fdw.Results;
using Fdw.Services.Settings.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings;

/// <summary>
/// Gives a connection or a secret manager its command timeout (FDW-3284): the number it states, else the instance's setting
/// <see cref="SettingDefinitions.DefaultCommandTimeoutSeconds"/>. No number is assumed in code or in a column, so one that has neither is a failure
/// that names it and the setting.
/// </summary>
public static class InstanceCommandTimeout
{
    private static readonly TimeoutMessages Messages = new(
        CommandTimeoutLog.NotPositive, CommandTimeoutLog.NoSettings, CommandTimeoutLog.ReadThroughItself, CommandTimeoutLog.NotStated);

    /// <summary>Resolves the command timeout.</summary>
    /// <param name="stated">The command timeout the connection or secret manager states, or <see langword="null"/> when it states none.</param>
    /// <param name="settings">The host's settings, read when the owner states none; <see langword="null"/> in a host that registers no settings.</param>
    /// <param name="ownerKind">What is being built, as a person says it: "connection" or "secret manager".</param>
    /// <param name="ownerName">The name of the connection or secret manager.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The command timeout in seconds, positive; a failure naming the owner, and the setting when it is the one missing.</returns>
    public static IGenericResult<int> Resolve(int? stated, Lazy<IEffectiveSettingsProvider>? settings, string ownerKind, string ownerName, ILogger logger)
        => InstanceTimeout.Resolve(stated, settings, SettingDefinitions.DefaultCommandTimeoutSeconds, ownerKind, ownerName, logger, Messages);
}
