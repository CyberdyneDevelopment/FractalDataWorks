using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Data.UI.Components.Logging;

/// <summary>
/// MessageLogging for DataCommandProvider operations.
/// EventId range: 4300-4319
/// </summary>
[MessageLoggingTypeCode("UICOMPONENTS")]
public static partial class DataCommandProviderLog
{
    /// <summary>
    /// Logs that the data command provider is initializing for the specified connection.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="connectionName">The name of the connection the provider is initializing for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace, Message = "DataCommandProvider initializing for connection '{connectionName}'")]
    public static partial IGenericMessage Initializing(ILogger logger, string connectionName);

    /// <summary>
    /// Logs that the data command provider is loading containers for the specified DataStore.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="dataStoreName">The name of the DataStore whose containers are being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace, Message = "DataCommandProvider loading containers for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage LoadingContainers(ILogger logger, string dataStoreName);

    /// <summary>
    /// Logs that the data command provider loaded the specified number of containers from the DataStore.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="containerCount">The number of containers loaded from the DataStore.</param>
    /// <param name="dataStoreName">The name of the DataStore the containers were loaded from.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information, Message = "DataCommandProvider loaded {containerCount} containers from DataStore '{dataStoreName}'")]
    public static partial IGenericMessage ContainersLoaded(ILogger logger, int containerCount, string dataStoreName);

    /// <summary>
    /// Logs that no containers were found for the specified connection because no DataStore is registered for it.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="connectionName">The name of the connection for which no containers were found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning, Message = "DataCommandProvider: no containers found for connection '{connectionName}' — no DataStore registered for this connection")]
    public static partial IGenericMessage NoContainersFound(ILogger logger, string connectionName);

    /// <summary>
    /// Logs that the data command provider failed to load containers for the specified connection.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="exception">The exception that caused the container load to fail.</param>
    /// <param name="connectionName">The name of the connection whose containers failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "DataCommandProvider failed to load containers for connection '{connectionName}'")]
    public static partial IGenericMessage LoadContainersFailed(ILogger logger, Exception exception, string connectionName);

    /// <summary>
    /// Logs that the data command provider is serializing the spec for the specified kind.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="kind">The kind of spec being serialized.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace, Message = "DataCommandProvider serializing spec for kind '{kind}'")]
    public static partial IGenericMessage SerializingSpec(ILogger logger, string kind);

    /// <summary>
    /// Logs that the specified skin type does not implement <c>ICommandBuilderSkin</c> so rendering was skipped.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="skinTypeName">The name of the skin type that does not implement the required interface.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91000, Level = LogLevel.Warning, Message = "DataCommandProvider: skin type '{skinTypeName}' does not implement ICommandBuilderSkin — rendering skipped")]
    public static partial IGenericMessage SkinTypeMismatch(ILogger logger, string skinTypeName);

    /// <summary>
    /// Logs that the data command provider spec changed for the specified kind.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="kind">The kind of spec that changed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace, Message = "DataCommandProvider spec changed — kind '{kind}'")]
    public static partial IGenericMessage SpecChanged(ILogger logger, string kind);
}
