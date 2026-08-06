using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Components.Logging;

/// <summary>
/// MessageLogging for ConnectionEditorProvider operations.
/// EventId range: 4220-4234
/// </summary>
[MessageLoggingTypeCode("COMPONENTS8")]
public static partial class ConnectionEditorProviderLog
{
    /// <summary>
    /// Logs that the connection editor is loading the named connection.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="name">The name of the connection being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "Loading connection '{name}' for editor")]
    public static partial IGenericMessage LoadingConnection(ILogger logger, string name);

    /// <summary>
    /// Logs that the connection editor finished loading the named connection.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="name">The name of the connection that was loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Loaded connection '{name}' for editor")]
    public static partial IGenericMessage LoadedConnection(ILogger logger, string name);

    /// <summary>
    /// Logs that the connection editor failed to load the named connection.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="name">The name of the connection that could not be loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "Failed to load connection '{name}' for editor")]
    public static partial IGenericMessage LoadConnectionFailed(ILogger logger, string name);

    /// <summary>
    /// Logs that the connection editor is loading the available connection types.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Trace,
        Message = "Loading connection types for editor")]
    public static partial IGenericMessage LoadingConnectionTypes(ILogger logger);

    /// <summary>
    /// Logs that the connection editor loaded the given number of connection types.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="count">The number of connection types that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Loaded {count} connection types for editor")]
    public static partial IGenericMessage LoadedConnectionTypes(ILogger logger, int count);

    /// <summary>
    /// Logs that the connection editor failed to load the available connection types.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Error,
        Message = "Failed to load connection types for editor")]
    public static partial IGenericMessage LoadConnectionTypesFailed(ILogger logger);

    /// <summary>
    /// Logs that the connection editor is loading authentication types for the given service type.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="serviceType">The service type whose authentication types are being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Trace,
        Message = "Loading authentication types for service type '{serviceType}'")]
    public static partial IGenericMessage LoadingAuthTypes(ILogger logger, string serviceType);

    /// <summary>
    /// Logs that the connection editor loaded the given number of authentication types for the service type.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="count">The number of authentication types that were loaded.</param>
    /// <param name="serviceType">The service type whose authentication types were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Debug,
        Message = "Loaded {count} authentication types for service type '{serviceType}'")]
    public static partial IGenericMessage LoadedAuthTypes(ILogger logger, int count, string serviceType);

    /// <summary>
    /// Logs that the connection editor failed to load authentication types for the given service type.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="serviceType">The service type whose authentication types could not be loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71002,
        Level = LogLevel.Warning,
        Message = "Failed to load authentication types for service type '{serviceType}'")]
    public static partial IGenericMessage LoadAuthTypesFailed(ILogger logger, string serviceType);

    /// <summary>
    /// Logs that the connection editor is creating the named connection.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="name">The name of the connection being created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Information,
        Message = "Creating connection '{name}'")]
    public static partial IGenericMessage CreatingConnection(ILogger logger, string name);

    /// <summary>
    /// Logs that the named connection was created successfully.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="name">The name of the connection that was created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Information,
        Message = "Connection '{name}' created successfully")]
    public static partial IGenericMessage ConnectionCreated(ILogger logger, string name);

    /// <summary>
    /// Logs that the connection editor failed to create the named connection.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="name">The name of the connection that could not be created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71003,
        Level = LogLevel.Error,
        Message = "Failed to create connection '{name}'")]
    public static partial IGenericMessage CreateConnectionFailed(ILogger logger, string name);

    /// <summary>
    /// Logs that an exception occurred while creating the named connection.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="exception">The exception that was raised while creating the connection.</param>
    /// <param name="name">The name of the connection that was being created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71004,
        Level = LogLevel.Error,
        Message = "Exception creating connection '{name}'")]
    public static partial IGenericMessage CreateConnectionException(ILogger logger, Exception exception, string name);

    /// <summary>
    /// Logs that the connection editor is updating the named connection.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="name">The name of the connection being updated.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "Updating connection '{name}'")]
    public static partial IGenericMessage UpdatingConnection(ILogger logger, string name);

    /// <summary>
    /// Logs that the named connection was updated successfully.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="name">The name of the connection that was updated.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Information,
        Message = "Connection '{name}' updated successfully")]
    public static partial IGenericMessage ConnectionUpdated(ILogger logger, string name);
}
