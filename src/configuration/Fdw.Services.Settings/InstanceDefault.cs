using System;
using Fdw.Results;
using Fdw.Services.Settings.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings;

/// <summary>
/// Gives a value its default from configuration (FDW-3580): the value the caller states, else the effective setting that holds the default,
/// read through the settings provider for the caller's tenant so the tenant, organization and workspace scope chain applies. No value is
/// assumed in code, so one that has neither is a failure naming the owner, the field and the setting.
/// </summary>
/// <remarks>
/// Mike, 2026-11-04: "I don't care what you set the default to as long as it is in configuration and not hardcoded", and "it should probably be
/// set in the configuration provider somehow so you don't have scope issues". A create path calls this for each value its request may leave
/// out, so the stored row always states the value it was given.
/// </remarks>
public static class InstanceDefault
{
    /// <summary>Resolves a value type.</summary>
    /// <typeparam name="T">The value's type, as the setting's value reads.</typeparam>
    /// <param name="stated">The value the caller states, or <see langword="null"/> when it states none.</param>
    /// <param name="settings">The host's settings; <see langword="null"/> in a host that registers no settings.</param>
    /// <param name="setting">The name of the setting that holds the default.</param>
    /// <param name="tenantId">The caller's tenant, whose scope the default is read in; <see langword="null"/> reads the server's.</param>
    /// <param name="ownerKind">What is being made, as a person says it ("schedule", "notification rule").</param>
    /// <param name="ownerName">The name of what is being made.</param>
    /// <param name="field">The value's name, as a person says it.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The value; a failure naming the owner, the field and the setting when neither is there.</returns>
    public static IGenericResult<T> Resolve<T>(
        T? stated, Lazy<IEffectiveSettingsProvider>? settings, string setting, Guid? tenantId, string ownerKind, string ownerName, string field, ILogger logger)
        where T : struct
        => ResolveForType(stated, settings, setting, typeName: null, tenantId, ownerKind, ownerName, field, logger);

    /// <summary>
    /// Resolves a value type whose default is layered by type (FDW-3823): the stated value, else the type's setting
    /// (<see cref="ForType"/>), else the server-wide setting.
    /// </summary>
    /// <typeparam name="T">The value's type, as the setting's value reads.</typeparam>
    /// <param name="stated">The value the caller states, or <see langword="null"/> when it states none.</param>
    /// <param name="settings">The host's settings; <see langword="null"/> in a host that registers no settings.</param>
    /// <param name="setting">The name of the server-wide setting that holds the default.</param>
    /// <param name="typeName">The connection, service or other type whose own setting overrides the server-wide one; <see langword="null"/> when none does.</param>
    /// <param name="tenantId">The caller's tenant, whose scope the default is read in; <see langword="null"/> reads the server's.</param>
    /// <param name="ownerKind">What is being made, as a person says it.</param>
    /// <param name="ownerName">The name of what is being made.</param>
    /// <param name="field">The value's name, as a person says it.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The value; a failure naming the owner, the field and the server-wide setting when none is there.</returns>
    /// <remarks>
    /// Mike, 2026-11-04: "clearly you should have a default server value ... connection type should override those for each connection type or
    /// maybe service type". A type that needs its own value gets a row named <c>{setting}.{typeName}</c>; every other type reads the server's.
    /// </remarks>
    public static IGenericResult<T> ResolveForType<T>(
        T? stated, Lazy<IEffectiveSettingsProvider>? settings, string setting, string? typeName, Guid? tenantId, string ownerKind, string ownerName, string field,
        ILogger logger)
        where T : struct
    {
        if (stated is { } own)
        {
            return GenericResult<T>.Success(own);
        }

        var provider = Provider(settings, setting, ownerKind, ownerName, field, logger);
        if (provider.IsFailure)
        {
            return provider.ToNewResult<T>();
        }

        if (provider.Value is { } read)
        {
            if (!string.IsNullOrWhiteSpace(typeName) && read.TryGetEffectiveValue<T>(ForType(setting, typeName), out var typed, tenantId))
            {
                return GenericResult<T>.Success(typed);
            }

            if (read.TryGetEffectiveValue<T>(setting, out var server, tenantId))
            {
                return GenericResult<T>.Success(server);
            }
        }

        return GenericResult<T>.Failure(InstanceDefaultLog.NotStated(logger, ownerKind, ownerName, field, setting));
    }

    /// <summary>Resolves text: an empty or blank value counts as not stated.</summary>
    /// <param name="stated">The text the caller states, or <see langword="null"/> or blank when it states none.</param>
    /// <param name="settings">The host's settings; <see langword="null"/> in a host that registers no settings.</param>
    /// <param name="setting">The name of the setting that holds the default.</param>
    /// <param name="tenantId">The caller's tenant, whose scope the default is read in; <see langword="null"/> reads the server's.</param>
    /// <param name="ownerKind">What is being made, as a person says it.</param>
    /// <param name="ownerName">The name of what is being made.</param>
    /// <param name="field">The value's name, as a person says it.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The text, not blank; a failure naming the owner, the field and the setting when neither is there.</returns>
    public static IGenericResult<string> ResolveText(
        string? stated, Lazy<IEffectiveSettingsProvider>? settings, string setting, Guid? tenantId, string ownerKind, string ownerName, string field, ILogger logger)
        => ResolveTextForType(stated, settings, setting, typeName: null, tenantId, ownerKind, ownerName, field, logger);

    /// <summary>Resolves text whose default is layered by type (FDW-3823): an empty or blank value counts as not stated.</summary>
    /// <param name="stated">The text the caller states, or <see langword="null"/> or blank when it states none.</param>
    /// <param name="settings">The host's settings; <see langword="null"/> in a host that registers no settings.</param>
    /// <param name="setting">The name of the server-wide setting that holds the default.</param>
    /// <param name="typeName">The type whose own setting overrides the server-wide one; <see langword="null"/> when none does.</param>
    /// <param name="tenantId">The caller's tenant, whose scope the default is read in; <see langword="null"/> reads the server's.</param>
    /// <param name="ownerKind">What is being made, as a person says it.</param>
    /// <param name="ownerName">The name of what is being made.</param>
    /// <param name="field">The value's name, as a person says it.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The text, not blank; a failure naming the owner, the field and the server-wide setting when none is there.</returns>
    public static IGenericResult<string> ResolveTextForType(
        string? stated, Lazy<IEffectiveSettingsProvider>? settings, string setting, string? typeName, Guid? tenantId, string ownerKind, string ownerName,
        string field, ILogger logger)
    {
        if (!string.IsNullOrWhiteSpace(stated))
        {
            return GenericResult<string>.Success(stated);
        }

        var provider = Provider(settings, setting, ownerKind, ownerName, field, logger);
        if (provider.IsFailure)
        {
            return provider.ToNewResult<string>();
        }

        if (provider.Value is { } read)
        {
            if (!string.IsNullOrWhiteSpace(typeName) && read.TryGetEffectiveValue<string>(ForType(setting, typeName), out var typed, tenantId)
                && !string.IsNullOrWhiteSpace(typed))
            {
                return GenericResult<string>.Success(typed);
            }

            if (read.TryGetEffectiveValue<string>(setting, out var server, tenantId) && !string.IsNullOrWhiteSpace(server))
            {
                return GenericResult<string>.Success(server);
            }
        }

        return GenericResult<string>.Failure(InstanceDefaultLog.NotStated(logger, ownerKind, ownerName, field, setting));
    }

    /// <summary>The name of a type's own default setting, which overrides the server-wide <paramref name="setting"/>: <c>{setting}.{typeName}</c>.</summary>
    /// <param name="setting">The server-wide setting's name.</param>
    /// <param name="typeName">The type's name.</param>
    /// <returns>The type's setting name.</returns>
    public static string ForType(string setting, string typeName) => setting + "." + typeName;

    private static IGenericResult<IEffectiveSettingsProvider> Provider(
        Lazy<IEffectiveSettingsProvider>? settings, string setting, string ownerKind, string ownerName, string field, ILogger logger)
    {
        if (settings is null)
        {
            return GenericResult<IEffectiveSettingsProvider>.Failure(InstanceDefaultLog.NoSettings(logger, ownerKind, ownerName, field, setting));
        }

        try
        {
            return GenericResult<IEffectiveSettingsProvider>.Success(settings.Value);
        }
        catch (InvalidOperationException ex)
        {
            // Why: the settings are read through connections, so a connection that states no value while the connection the settings are read
            // through is being made asks for the provider it is itself a part of. That is a defect of its configuration, and it says so.
            return GenericResult<IEffectiveSettingsProvider>.Failure(InstanceDefaultLog.ReadThroughItselfThrew(logger, ex, ownerKind, ownerName, field, setting));
        }
    }
}
