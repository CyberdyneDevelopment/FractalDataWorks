using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.Components.Logging;

/// <summary>
/// MessageLogging for SecretManagerProvider operations.
/// EventId range: 4400-4414
/// </summary>
[MessageLoggingTypeCode("COMPONENTS15")]
public static partial class SecretManagerProviderLog
{
    /// <summary>
    /// Logs that secret managers are being loaded.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "Loading secret managers")]
    public static partial IGenericMessage LoadingSecretManagers(ILogger logger);

    /// <summary>
    /// Logs that secret managers were loaded, including the count.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="count">The number of secret managers that were loaded.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Loaded {count} secret managers")]
    public static partial IGenericMessage LoadedSecretManagers(ILogger logger, int count);

    /// <summary>
    /// Logs that loading secret managers failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "Failed to load secret managers")]
    public static partial IGenericMessage LoadSecretManagersFailed(ILogger logger);

    /// <summary>
    /// Logs that a single secret manager is being loaded.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the secret manager being loaded.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Trace,
        Message = "Loading secret manager '{name}'")]
    public static partial IGenericMessage LoadingSecretManager(ILogger logger, string name);

    /// <summary>
    /// Logs that a single secret manager was loaded.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the secret manager that was loaded.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Loaded secret manager '{name}'")]
    public static partial IGenericMessage LoadedSecretManager(ILogger logger, string name);

    /// <summary>
    /// Logs that loading a single secret manager failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the secret manager that failed to load.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Error,
        Message = "Failed to load secret manager '{name}'")]
    public static partial IGenericMessage LoadSecretManagerFailed(ILogger logger, string name);

    // Why (FDW-583): pre-action announcement, not a completed business milestone — noise at Info.
    /// <summary>
    /// Logs that a secret manager is being created.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the secret manager being created.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Debug,
        Message = "Creating secret manager '{name}'")]
    public static partial IGenericMessage CreatingSecretManager(ILogger logger, string name);

    /// <summary>
    /// Logs that a secret manager was created successfully.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the secret manager that was created.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Information,
        Message = "Secret manager '{name}' created successfully")]
    public static partial IGenericMessage SecretManagerCreated(ILogger logger, string name);

    /// <summary>
    /// Logs that creating a secret manager failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the secret manager that failed to create.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 71002,
        Level = LogLevel.Error,
        Message = "Failed to create secret manager '{name}'")]
    public static partial IGenericMessage CreateSecretManagerFailed(ILogger logger, string name);

    // Why (FDW-583): pre-action announcement, not a completed business milestone — noise at Info.
    /// <summary>
    /// Logs that a secret manager is being updated.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the secret manager being updated.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Debug,
        Message = "Updating secret manager '{name}'")]
    public static partial IGenericMessage UpdatingSecretManager(ILogger logger, string name);

    /// <summary>
    /// Logs that a secret manager was updated successfully.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the secret manager that was updated.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Information,
        Message = "Secret manager '{name}' updated successfully")]
    public static partial IGenericMessage SecretManagerUpdated(ILogger logger, string name);

    /// <summary>
    /// Logs that updating a secret manager failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the secret manager that failed to update.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 71003,
        Level = LogLevel.Error,
        Message = "Failed to update secret manager '{name}'")]
    public static partial IGenericMessage UpdateSecretManagerFailed(ILogger logger, string name);

    // Why (FDW-583): pre-action announcement, not a completed business milestone — noise at Info.
    /// <summary>
    /// Logs that a secret manager is being deleted.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the secret manager being deleted.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Debug,
        Message = "Deleting secret manager '{name}'")]
    public static partial IGenericMessage DeletingSecretManager(ILogger logger, string name);

    /// <summary>
    /// Logs that a secret manager was deleted successfully.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the secret manager that was deleted.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Information,
        Message = "Secret manager '{name}' deleted successfully")]
    public static partial IGenericMessage SecretManagerDeleted(ILogger logger, string name);

    /// <summary>
    /// Logs that deleting a secret manager failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the secret manager that failed to delete.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 71004,
        Level = LogLevel.Error,
        Message = "Failed to delete secret manager '{name}'")]
    public static partial IGenericMessage DeleteSecretManagerFailed(ILogger logger, string name);
}
