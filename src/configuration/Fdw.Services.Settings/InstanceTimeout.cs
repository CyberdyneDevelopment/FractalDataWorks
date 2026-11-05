using System;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings;

/// <summary>
/// Resolves a timeout an owner may state or leave to the instance: the number it states, else the instance's setting. No number is assumed in code
/// or in a column, so one that has neither is a failure that names it and the setting (FDW-3284, FDW-3580).
/// </summary>
/// <remarks>
/// Why one resolver: the command timeout (<see cref="InstanceCommandTimeout"/>) and the connect timeout (<see cref="InstanceConnectionTimeout"/>)
/// follow the same rule, and each says so in its own words through its <see cref="TimeoutMessages"/>.
/// </remarks>
internal static class InstanceTimeout
{
    /// <summary>Resolves the timeout.</summary>
    /// <param name="stated">The timeout the owner states, or <see langword="null"/> when it states none.</param>
    /// <param name="settings">The host's settings, read when the owner states none; <see langword="null"/> in a host that registers no settings.</param>
    /// <param name="setting">The name of the instance setting that gives the timeout an owner does not state.</param>
    /// <param name="ownerKind">What is being built, as a person says it.</param>
    /// <param name="ownerName">The name of what is being built.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="messages">The failures, in this timeout's words.</param>
    /// <returns>The timeout in seconds, positive; a failure naming the owner, and the setting when it is the one missing.</returns>
    internal static IGenericResult<int> Resolve(
        int? stated, Lazy<IEffectiveSettingsProvider>? settings, string setting, string ownerKind, string ownerName, ILogger logger, TimeoutMessages messages)
    {
        ArgumentNullException.ThrowIfNull(setting);
        ArgumentNullException.ThrowIfNull(ownerKind);
        ArgumentNullException.ThrowIfNull(ownerName);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(messages);

        if (stated is { } own)
        {
            return own > 0
                ? GenericResult<int>.Success(own)
                : GenericResult<int>.Failure(messages.NotPositive(logger, ownerKind, ownerName, own));
        }

        if (settings is null)
        {
            return GenericResult<int>.Failure(messages.NoSettings(logger, ownerKind, ownerName, setting));
        }

        IEffectiveSettingsProvider provider;
        try
        {
            provider = settings.Value;
        }
        catch (InvalidOperationException)
        {
            // Why: the settings are read through connections, so an owner that states no timeout while the connection the settings are read
            // through is being built asks for the provider it is itself a part of. That is a defect of its configuration, and it says so.
            return GenericResult<int>.Failure(messages.ReadThroughItself(logger, ownerKind, ownerName, setting));
        }

        return provider.TryGetEffectiveValue<int>(setting, out var instance) && instance > 0
            ? GenericResult<int>.Success(instance)
            : GenericResult<int>.Failure(messages.NotStated(logger, ownerKind, ownerName, setting));
    }
}
