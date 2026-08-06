using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Credentials.Logging;

/// <summary>
/// MessageLogging for CredentialService operations.
/// EventId range: 4547-4599
/// </summary>
[MessageLoggingTypeCode("CREDENTIALS")]
public static partial class CredentialServiceLog
{
    /// <summary>
    /// Logs that execution of a vault command has started on a credential service.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="commandType">The type of vault command being executed.</param>
    /// <param name="serviceName">The name of the credential service the command runs on.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "Executing vault command '{commandType}' on credential service '{serviceName}'")]
    public static partial IGenericMessage ExecuteStarted(ILogger logger, string commandType, string serviceName);

    /// <summary>
    /// Logs that a vault command succeeded on a credential service.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="commandType">The type of vault command that succeeded.</param>
    /// <param name="serviceName">The name of the credential service the command ran on.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug,
        Message = "Vault command '{commandType}' succeeded on credential service '{serviceName}'")]
    public static partial IGenericMessage ExecuteSucceeded(ILogger logger, string commandType, string serviceName);

    /// <summary>
    /// Logs that a vault command failed on a credential service.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the vault command to fail.</param>
    /// <param name="commandType">The type of vault command that failed.</param>
    /// <param name="serviceName">The name of the credential service the command ran on.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "Vault command '{commandType}' failed on credential service '{serviceName}'")]
    public static partial IGenericMessage ExecuteFailed(ILogger logger, Exception exception, string commandType, string serviceName);

    /// <summary>
    /// Logs that the credential service command is null and cannot be executed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error,
        Message = "Credential service command is null — a null command cannot be executed")]
    public static partial IGenericMessage CommandNull(ILogger logger);

    /// <summary>
    /// Logs that the credential service request is empty and must supply either an Id or a Name.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21001, Level = LogLevel.Error,
        Message = "Empty credential service request — request must supply either Id or Name")]
    public static partial IGenericMessage EmptyCredentialServiceRequest(ILogger logger);

    /// <summary>
    /// Logs that the configured credential service name is missing and no default is applied.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61000, Level = LogLevel.Error,
        Message = "Configured credential service name is missing — a credential service name is required, no default is applied")]
    public static partial IGenericMessage CredentialServiceNameMissing(ILogger logger);

    /// <summary>
    /// Logs that resolving a credential service by name failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="serviceName">The name of the credential service that could not be resolved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31000, Level = LogLevel.Error,
        Message = "Failed to resolve credential service '{serviceName}'")]
    public static partial IGenericMessage CredentialServiceResolveFailed(ILogger logger, string serviceName);

    /// <summary>
    /// Logs that the typed credential service body is missing even though the ServiceOptionType resolved.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="serviceName">The name of the credential service whose typed body was not populated.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61001, Level = LogLevel.Error,
        Message = "Typed credential service body is missing for service '{serviceName}' — ServiceOptionType resolved but the typed configuration was not populated")]
    public static partial IGenericMessage TypedBodyMissing(ILogger logger, string serviceName);

    /// <summary>
    /// Logs that the CredentialVaultName is empty on a credential service's typed configuration.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="serviceName">The name of the credential service whose typed configuration has no vault name.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61002, Level = LogLevel.Error,
        Message = "CredentialVaultName is empty on credential service '{serviceName}' — the typed configuration has no vault name")]
    public static partial IGenericMessage CredentialVaultNameMissing(ILogger logger, string serviceName);

    /// <summary>
    /// Logs that resolving a credential vault for a credential service failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the credential vault that could not be resolved.</param>
    /// <param name="serviceName">The name of the credential service the vault belongs to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "Failed to resolve credential vault '{vaultName}' for credential service '{serviceName}'")]
    public static partial IGenericMessage CredentialVaultResolveFailed(ILogger logger, string vaultName, string serviceName);

    /// <summary>
    /// Logs that a credential vault was resolved for a credential service.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the credential vault that was resolved.</param>
    /// <param name="serviceName">The name of the credential service the vault belongs to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug,
        Message = "Credential vault '{vaultName}' resolved for credential service '{serviceName}'")]
    public static partial IGenericMessage CredentialVaultResolved(ILogger logger, string vaultName, string serviceName);

    /// <summary>
    /// Logs that a typed cache was registered for a credential service ServiceOptionType.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="serviceOptionType">The service option type the typed cache was registered for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug,
        Message = "Registered typed cache for credential service ServiceOptionType '{serviceOptionType}'")]
    public static partial IGenericMessage TypedCacheRegistered(ILogger logger, string serviceOptionType);

    /// <summary>
    /// Logs that a credential service has no ServiceOptionType, so its typed configuration cannot be resolved.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="serviceName">The name of the credential service missing a ServiceOptionType.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61003, Level = LogLevel.Warning,
        Message = "Credential service '{serviceName}' has no ServiceOptionType — cannot resolve typed configuration")]
    public static partial IGenericMessage NoServiceOptionType(ILogger logger, string serviceName);

    /// <summary>
    /// Logs that no typed credential service provider is registered for the requested service option type.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="serviceOptionType">The service option type for which no typed provider was found.</param>
    /// <param name="serviceName">The name of the credential service that could not be loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61004, Level = LogLevel.Error,
        Message = "No typed credential service provider registered for service option type '{serviceOptionType}' (service '{serviceName}')")]
    public static partial IGenericMessage NoTypedProviderForServiceOptionType(ILogger logger, string serviceOptionType, string serviceName);

    /// <summary>
    /// Logs that loading the typed credential service body failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the typed body load to fail.</param>
    /// <param name="serviceName">The name of the credential service whose typed body failed to load.</param>
    /// <param name="serviceOptionType">The service option type used when the load failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "Failed to load typed credential service body for '{serviceName}' (service option type '{serviceOptionType}')")]
    public static partial IGenericMessage TypedBodyLoadFailed(ILogger logger, Exception exception, string serviceName, string serviceOptionType);

    /// <summary>
    /// Logs that the typed credential service body was successfully loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="serviceName">The name of the credential service whose typed body was loaded.</param>
    /// <param name="serviceOptionType">The service option type used to load the typed body.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "Typed credential service body loaded for '{serviceName}' (service option type '{serviceOptionType}')")]
    public static partial IGenericMessage TypedBodyLoaded(ILogger logger, string serviceName, string serviceOptionType);

    /// <summary>
    /// Logs that lookup of the typed configuration for a credential service has started.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="serviceName">The name of the credential service whose typed configuration is being looked up.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace,
        Message = "Looking up typed configuration for credential service '{serviceName}'")]
    public static partial IGenericMessage TypedLookupStarted(ILogger logger, string serviceName);

    /// <summary>
    /// Logs that the credential service factory configuration is invalid because the typed body is missing or the wrong type.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="serviceName">The name of the credential service whose factory configuration is invalid.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61005, Level = LogLevel.Error,
        Message = "Credential service factory configuration is invalid for '{serviceName}' — typed body is missing or wrong type")]
    public static partial IGenericMessage FactoryConfigurationInvalid(ILogger logger, string serviceName);

    /// <summary>
    /// Logs that the resolved vault for a credential service is not an ICredentialVault (the configured vault is the wrong type).
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the resolved vault that is the wrong type.</param>
    /// <param name="serviceName">The name of the credential service the vault belongs to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 41000, Level = LogLevel.Error,
        Message = "Resolved vault '{vaultName}' for credential service '{serviceName}' is not an ICredentialVault — the configured vault is the wrong type")]
    public static partial IGenericMessage VaultNotCredentialVault(ILogger logger, string vaultName, string serviceName);
}
