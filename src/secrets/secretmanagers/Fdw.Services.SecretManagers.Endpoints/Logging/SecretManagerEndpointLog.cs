using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.Endpoints.Logging;

/// <summary>
/// MessageLogging for secret manager endpoint operations.
/// EventId range: 7232-7260
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS11")]
public static partial class SecretManagerEndpointLog
{
    /// <summary>Logs listing secret managers.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug,
        Message = "Listing secret managers")]
    public static partial IGenericMessage ListingSecretManagers(ILogger logger);

    /// <summary>Logs the count of secret managers listed.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "Found {count} secret manager(s)")]
    public static partial IGenericMessage SecretManagersListed(ILogger logger, int count);

    /// <summary>Logs getting a secret manager by name.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug,
        Message = "Getting secret manager '{name}'")]
    public static partial IGenericMessage GettingSecretManager(ILogger logger, string name);

    /// <summary>Logs when a secret manager is not found.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning,
        Message = "Secret manager '{name}' not found")]
    public static partial IGenericMessage SecretManagerNotFound(ILogger logger, string name);

    /// <summary>Logs creating a secret manager.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug,
        Message = "Creating secret manager '{name}' of type '{secretManagerType}'")]
    public static partial IGenericMessage CreatingSecretManager(ILogger logger, string name, string secretManagerType);

    /// <summary>Logs when a duplicate secret manager name is detected.</summary>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning,
        Message = "Secret manager '{name}' already exists")]
    public static partial IGenericMessage SecretManagerAlreadyExists(ILogger logger, string name);

    /// <summary>Logs successful creation of a secret manager.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information,
        Message = "Secret manager '{name}' created successfully")]
    public static partial IGenericMessage SecretManagerCreated(ILogger logger, string name);

    /// <summary>Logs updating a secret manager.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Debug,
        Message = "Updating secret manager '{name}'")]
    public static partial IGenericMessage UpdatingSecretManager(ILogger logger, string name);

    /// <summary>Logs successful update of a secret manager.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information,
        Message = "Secret manager '{name}' updated successfully")]
    public static partial IGenericMessage SecretManagerUpdated(ILogger logger, string name);

    /// <summary>Logs deleting a secret manager.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Debug,
        Message = "Deleting secret manager '{name}'")]
    public static partial IGenericMessage DeletingSecretManager(ILogger logger, string name);

    /// <summary>Logs successful deletion of a secret manager.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Information,
        Message = "Secret manager '{name}' deleted successfully")]
    public static partial IGenericMessage SecretManagerDeleted(ILogger logger, string name);

    /// <summary>Logs a failure to create a configuration writer.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error,
        Message = "Failed to create configuration writer for secret manager")]
    public static partial IGenericMessage WriterCreationFailed(ILogger logger);

    /// <summary>Logs a secret manager type the caller named that is not a known type.</summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Warning,
        Message = "Secret manager type '{typeName}' is not a known type. Known types: {known}",
        ClientMessage = "Secret manager type '{typeName}' is not one this host offers. It offers: {known}")]
    public static partial IGenericMessage SecretManagerTypeUnknown(ILogger logger, string typeName, string known);

    /// <summary>Logs a setting the caller sent that the chosen type does not take.</summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Warning,
        Message = "Setting '{member}' is not one a {typeName} secret manager takes. It takes: {takes}",
        ClientMessage = "'{member}' is not a setting of a {typeName} secret manager. It takes: {takes}")]
    public static partial IGenericMessage SecretManagerSettingUnknown(ILogger logger, string typeName, string member, string takes);

    /// <summary>Logs settings that were not sent as an object, or could not be read as the chosen type's.</summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Warning,
        Message = "The settings for a {typeName} secret manager could not be read: {reason}",
        ClientMessage = "The settings for a {typeName} secret manager could not be read: {reason}")]
    public static partial IGenericMessage SecretManagerSettingsUnreadable(ILogger logger, string typeName, string reason);

    /// <summary>Logs a setting sent as null that always has a value, so it cannot be cleared.</summary>
    [MessageLogging(EventId = 21003, Level = LogLevel.Warning,
        Message = "Setting '{member}' of a {typeName} secret manager was sent as null, but it always has a value",
        ClientMessage = "'{member}' cannot be cleared: a {typeName} secret manager always has a value for it. Send a value")]
    public static partial IGenericMessage SecretManagerSettingNotClearable(ILogger logger, string typeName, string member);

    /// <summary>Logs an unexpected error during secret manager endpoint handling.</summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error,
        Message = "Unexpected error in secret manager endpoint")]
    public static partial IGenericMessage UnexpectedError(ILogger logger, Exception exception);

    /// <summary>Logs when a modification is rejected because the secret manager is a system configuration.</summary>
    [MessageLogging(EventId = 41001, Level = LogLevel.Warning,
        Message = "Rejected modification of system secret manager '{secretManagerName}' — system configurations are read-only")]
    public static partial IGenericMessage SystemSecretManagerReadOnly(ILogger logger, string secretManagerName);
}
