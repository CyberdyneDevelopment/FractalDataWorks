using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Components.Logging;

/// <summary>
/// MessageLogging for ConnectionWizardProvider operations.
/// EventId range: 4235-4255
/// </summary>
[MessageLoggingTypeCode("COMPONENTS8")]
public static partial class ConnectionWizardProviderLog
{
    /// <summary>
    /// Logs that the connection wizard is loading the available connection types.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Trace,
        Message = "Loading connection types for wizard")]
    public static partial IGenericMessage LoadingConnectionTypes(ILogger logger);

    /// <summary>
    /// Logs that the connection wizard loaded the given number of connection types.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="count">The number of connection types that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Debug,
        Message = "Loaded {count} connection types for wizard")]
    public static partial IGenericMessage LoadedConnectionTypes(ILogger logger, int count);

    /// <summary>
    /// Logs that the connection wizard advanced to the given step.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="step">The step number the wizard advanced to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Debug,
        Message = "Wizard advanced to step {step}")]
    public static partial IGenericMessage StepAdvanced(ILogger logger, int step);

    /// <summary>
    /// Logs that the connection wizard stepped back to the given step.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="step">The step number the wizard stepped back to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Debug,
        Message = "Wizard stepped back to step {step}")]
    public static partial IGenericMessage StepBack(ILogger logger, int step);

    /// <summary>
    /// Logs that the connection wizard is loading authentication types for the given service type.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="serviceType">The service type whose authentication types are being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Trace,
        Message = "Loading authentication types for service type '{serviceType}'")]
    public static partial IGenericMessage LoadingAuthTypes(ILogger logger, string serviceType);

    /// <summary>
    /// Logs that the connection wizard loaded the given number of authentication types for the service type.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="count">The number of authentication types that were loaded.</param>
    /// <param name="serviceType">The service type whose authentication types were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Debug,
        Message = "Loaded {count} authentication types for service type '{serviceType}'")]
    public static partial IGenericMessage LoadedAuthTypes(ILogger logger, int count, string serviceType);

    /// <summary>
    /// Logs that the connection wizard is testing the configuration for the named connection.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="name">The name of the connection being tested.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11018,
        Level = LogLevel.Information,
        Message = "Testing connection configuration for '{name}'")]
    public static partial IGenericMessage TestingConnection(ILogger logger, string name);

    /// <summary>
    /// Logs that the connection test succeeded for the named connection.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="name">The name of the connection whose test succeeded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11019,
        Level = LogLevel.Information,
        Message = "Connection test succeeded for '{name}'")]
    public static partial IGenericMessage TestConnectionSucceeded(ILogger logger, string name);

    /// <summary>
    /// Logs that the connection test failed for the named connection.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="name">The name of the connection whose test failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71025,
        Level = LogLevel.Warning,
        Message = "Connection test failed for '{name}'")]
    public static partial IGenericMessage TestConnectionFailed(ILogger logger, string name);

    /// <summary>
    /// Logs that an exception occurred while testing the named connection.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="exception">The exception that was raised while testing the connection.</param>
    /// <param name="name">The name of the connection that was being tested.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71026,
        Level = LogLevel.Error,
        Message = "Exception testing connection '{name}'")]
    public static partial IGenericMessage TestConnectionException(ILogger logger, Exception exception, string name);

    /// <summary>
    /// Logs that the connection wizard is saving the named connection.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="name">The name of the connection being saved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11020,
        Level = LogLevel.Information,
        Message = "Saving connection '{name}'")]
    public static partial IGenericMessage SavingConnection(ILogger logger, string name);

    /// <summary>
    /// Logs that the named connection was saved successfully.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="name">The name of the connection that was saved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11021,
        Level = LogLevel.Information,
        Message = "Connection '{name}' saved successfully")]
    public static partial IGenericMessage ConnectionSaved(ILogger logger, string name);

    /// <summary>
    /// Logs that the connection wizard failed to save the named connection.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="name">The name of the connection that could not be saved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71027,
        Level = LogLevel.Error,
        Message = "Failed to save connection '{name}'")]
    public static partial IGenericMessage SaveConnectionFailed(ILogger logger, string name);

    /// <summary>
    /// Logs that an exception occurred while saving the named connection.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="exception">The exception that was raised while saving the connection.</param>
    /// <param name="name">The name of the connection that was being saved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71028,
        Level = LogLevel.Error,
        Message = "Exception saving connection '{name}'")]
    public static partial IGenericMessage SaveConnectionException(ILogger logger, Exception exception, string name);

    /// <summary>
    /// Logs that the connection wizard completed for the named connection.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="name">The name of the connection for which the wizard completed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11022,
        Level = LogLevel.Information,
        Message = "Connection wizard completed for '{name}'")]
    public static partial IGenericMessage WizardCompleted(ILogger logger, string name);

    // Secret Manager (4250-4256)

    /// <summary>
    /// Logs that the connection wizard is loading the available secret managers.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11023,
        Level = LogLevel.Trace,
        Message = "Loading available secret managers")]
    public static partial IGenericMessage LoadingSecretManagers(ILogger logger);

    /// <summary>
    /// Logs that the connection wizard loaded the given number of secret managers.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="count">The number of secret managers that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11024,
        Level = LogLevel.Debug,
        Message = "Loaded {count} secret manager(s)")]
    public static partial IGenericMessage LoadedSecretManagers(ILogger logger, int count);

    /// <summary>
    /// Logs that a password secret is being stored in the named secret manager.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="managerName">The name of the secret manager in which the secret is being stored.</param>
    /// <param name="keyName">The key name of the password secret being stored.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11025,
        Level = LogLevel.Information,
        Message = "Storing password secret '{keyName}' in manager '{managerName}'")]
    public static partial IGenericMessage StoringSecret(ILogger logger, string managerName, string keyName);

    /// <summary>
    /// Logs that a password secret was stored in the named secret manager.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="managerName">The name of the secret manager in which the secret was stored.</param>
    /// <param name="keyName">The key name of the password secret that was stored.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11026,
        Level = LogLevel.Information,
        Message = "Password secret '{keyName}' stored in manager '{managerName}'")]
    public static partial IGenericMessage SecretStored(ILogger logger, string managerName, string keyName);

    /// <summary>
    /// Logs that the connection wizard failed to store a password secret in the named secret manager.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="managerName">The name of the secret manager in which the secret could not be stored.</param>
    /// <param name="keyName">The key name of the password secret that could not be stored.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71029,
        Level = LogLevel.Error,
        Message = "Failed to store password secret '{keyName}' in manager '{managerName}'")]
    public static partial IGenericMessage StoreSecretFailed(ILogger logger, string managerName, string keyName);

    /// <summary>
    /// Logs that an exception occurred while storing a password secret in the named secret manager.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="exception">The exception that was raised while storing the password secret.</param>
    /// <param name="managerName">The name of the secret manager in which the secret was being stored.</param>
    /// <param name="keyName">The key name of the password secret that was being stored.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71030,
        Level = LogLevel.Error,
        Message = "Exception storing password secret '{keyName}' in manager '{managerName}'")]
    public static partial IGenericMessage StoreSecretException(ILogger logger, Exception exception, string managerName, string keyName);
}
