using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Logging;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications;

/// <summary>
/// Sets the settings a notification type declares onto a configuration, refusing any the type does not declare (FDW-2931).
/// </summary>
/// <remarks>
/// The settings are set by the name of the property that holds them, through the configuration's mapper, so nothing here
/// names a notification type. A secret setting is never given as a value: it is satisfied by the notification naming a
/// secret manager and a key.
/// </remarks>
public static class NotificationSettings
{
    /// <summary>Sets the given values onto a configuration after checking each against the type's declared settings.</summary>
    /// <param name="type">The notification type, which declares the settings.</param>
    /// <param name="configuration">The configuration to set them on.</param>
    /// <param name="values">The values by setting name; none leaves the settings as they are.</param>
    /// <param name="creating">True when the notification is being made, so a required setting that is not given is refused.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <returns>Success, or the refusal naming the first setting that cannot be taken.</returns>
    public static IGenericResult Apply(
        INotificationType type,
        INotificationImplementationConfiguration configuration,
        IReadOnlyDictionary<string, JsonElement>? values,
        bool creating,
        ILogger logger)
    {
        if (values is not null)
        {
            foreach (var given in values)
            {
                var set = SetOne(type, configuration, given.Key, given.Value, logger);
                if (set.IsFailure)
                {
                    return set;
                }
            }
        }

        return creating ? Required(type, configuration, values, logger, defaulted: false) : GenericResult.Success();
    }

    /// <summary>
    /// Sets the given values onto a configuration being made, gives each declared setting that is not given and names a
    /// <see cref="NotificationSettingField.DefaultSetting"/> that setting's value for the caller's tenant, and then refuses a required one still missing (FDW-3580).
    /// </summary>
    /// <param name="type">The notification type, which declares the settings.</param>
    /// <param name="configuration">The configuration being made.</param>
    /// <param name="values">The values by setting name.</param>
    /// <param name="settings">The host's settings, which hold the defaults; <see langword="null"/> in a host that registers no settings.</param>
    /// <param name="tenantId">The caller's tenant, whose scope the defaults are read in.</param>
    /// <param name="name">The name of the notification being made.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <returns>Success, or the refusal naming the first setting that cannot be taken or has no value.</returns>
    public static IGenericResult ApplyNew(
        INotificationType type,
        INotificationImplementationConfiguration configuration,
        IReadOnlyDictionary<string, JsonElement>? values,
        Lazy<IEffectiveSettingsProvider>? settings,
        Guid? tenantId,
        string name,
        ILogger logger)
    {
        var given = Apply(type, configuration, values, creating: false, logger);
        if (given.IsFailure)
        {
            return given;
        }

        foreach (var field in type.Settings.Where(declared => declared.DefaultSetting is not null && !declared.IsSecret && !IsGiven(values, declared)))
        {
            var read = Default(field, settings, tenantId, type.Name, name, logger);
            if (read.IsFailure || read.Value is not { } value)
            {
                return GenericResult.Failure(read.NoValueReason());
            }

            PocoMapperCollection.ByName(configuration.GetType().Name).SetValue(configuration, field.Name, value);
        }

        return Required(type, configuration, values, logger, defaulted: true);
    }

    // The default of one declared setting, read as the setting's own data type.
    private static IGenericResult<object> Default(
        NotificationSettingField field, Lazy<IEffectiveSettingsProvider>? settings, Guid? tenantId, string typeName, string name, ILogger logger)
    {
        var setting = field.DefaultSetting ?? throw new ArgumentException("The field names no default setting.", nameof(field));
        var owner = typeName + " notification";
        return field.DataType switch
        {
            NotificationSettingTypes.Number => Boxed(InstanceDefault.Resolve<int>(null, settings, setting, tenantId, owner, name, field.Name, logger)),
            NotificationSettingTypes.Flag => Boxed(InstanceDefault.Resolve<bool>(null, settings, setting, tenantId, owner, name, field.Name, logger)),
            _ => Boxed(InstanceDefault.ResolveText(null, settings, setting, tenantId, owner, name, field.Name, logger)),
        };
    }

    private static IGenericResult<object> Boxed<T>(IGenericResult<T> read) =>
        read.IsSuccess && read.Value is { } value ? GenericResult<object>.Success(value) : read.ToNewResult<object>();

    private static bool IsGiven(IReadOnlyDictionary<string, JsonElement>? values, NotificationSettingField field) =>
        values is not null && values.Keys.Any(key => string.Equals(key, field.Name, StringComparison.OrdinalIgnoreCase));

    private static IGenericResult SetOne(
        INotificationType type, INotificationImplementationConfiguration configuration, string name, JsonElement value, ILogger logger)
    {
        var field = type.Settings.FirstOrDefault(declared => string.Equals(declared.Name, name, StringComparison.OrdinalIgnoreCase));
        if (field is null)
        {
            return GenericResult.Failure(NotificationSettingLog.SettingUnknown(logger, name, type.Name));
        }

        if (field.IsSecret)
        {
            return GenericResult.Failure(NotificationSettingLog.SettingIsSecret(logger, field.Name));
        }

        if (!TryRead(field, value, out var read))
        {
            return GenericResult.Failure(NotificationSettingLog.SettingNotReadable(logger, field.Name, field.DataType));
        }

        PocoMapperCollection.ByName(configuration.GetType().Name).SetValue(configuration, field.Name, read);
        return GenericResult.Success();
    }

    private static IGenericResult Required(
        INotificationType type, INotificationImplementationConfiguration configuration, IReadOnlyDictionary<string, JsonElement>? values, ILogger logger,
        bool defaulted)
    {
        foreach (var field in type.Settings.Where(declared => declared.IsRequired))
        {
            if (field.IsSecret)
            {
                if (string.IsNullOrWhiteSpace(configuration.SecretManagerName) || string.IsNullOrWhiteSpace(configuration.SecretKeyName))
                {
                    return GenericResult.Failure(NotificationSettingLog.SettingSecretNotNamed(logger, field.Name, type.Name));
                }

                continue;
            }

            // A setting that took its default (ApplyNew) has a value although it was not given.
            if (!IsGiven(values, field) && !(defaulted && field.DefaultSetting is not null))
            {
                return GenericResult.Failure(NotificationSettingLog.SettingMissing(logger, field.Name, type.Name));
            }
        }

        return GenericResult.Success();
    }

    private static bool TryRead(NotificationSettingField field, JsonElement value, out object? read)
    {
        read = null;
        switch (field.DataType)
        {
            case NotificationSettingTypes.Text when value.ValueKind == JsonValueKind.String:
                read = value.GetString();
                return true;
            case NotificationSettingTypes.Number when value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number):
                read = number;
                return true;
            case NotificationSettingTypes.Flag when value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                read = value.GetBoolean();
                return true;
            default:
                return false;
        }
    }
}
