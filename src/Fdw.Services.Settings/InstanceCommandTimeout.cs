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
    /// <summary>Resolves the command timeout.</summary>
    /// <param name="stated">The command timeout the connection or secret manager states, or <see langword="null"/> when it states none.</param>
    /// <param name="settings">The host's settings, read when the owner states none; <see langword="null"/> in a host that registers no settings.</param>
    /// <param name="ownerKind">What is being built, as a person says it: "connection" or "secret manager".</param>
    /// <param name="ownerName">The name of the connection or secret manager.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The command timeout in seconds, positive; a failure naming the owner, and the setting when it is the one missing.</returns>
    public static IGenericResult<int> Resolve(int? stated, Lazy<IEffectiveSettingsProvider>? settings, string ownerKind, string ownerName, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(ownerKind);
        ArgumentNullException.ThrowIfNull(ownerName);
        ArgumentNullException.ThrowIfNull(logger);

        if (stated is { } own)
        {
            return own > 0
                ? GenericResult<int>.Success(own)
                : GenericResult<int>.Failure(CommandTimeoutLog.NotPositive(logger, ownerKind, ownerName, own));
        }

        if (settings is null)
        {
            return GenericResult<int>.Failure(CommandTimeoutLog.NoSettings(logger, ownerKind, ownerName, SettingDefinitions.DefaultCommandTimeoutSeconds));
        }

        IEffectiveSettingsProvider provider;
        try
        {
            provider = settings.Value;
        }
        catch (InvalidOperationException)
        {
            // Why: the settings are read through connections, so a connection that states no timeout while the connection the settings are read
            // through is being built asks for the provider it is itself a part of. That is a defect of its configuration, and it says so.
            return GenericResult<int>.Failure(CommandTimeoutLog.ReadThroughItself(logger, ownerKind, ownerName, SettingDefinitions.DefaultCommandTimeoutSeconds));
        }

        return provider.TryGetEffectiveValue<int>(SettingDefinitions.DefaultCommandTimeoutSeconds, out var instance) && instance > 0
            ? GenericResult<int>.Success(instance)
            : GenericResult<int>.Failure(CommandTimeoutLog.NotStated(logger, ownerKind, ownerName, SettingDefinitions.DefaultCommandTimeoutSeconds));
    }
}
