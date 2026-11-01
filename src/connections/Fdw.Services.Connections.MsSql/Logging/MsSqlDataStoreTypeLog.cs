using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.Logging;

/// <summary>
/// MessageLogging for the MsSql data-store type registration.
/// EventId range: 5253-5259
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlDataStoreTypeLog
{
    /// <summary>
    /// Logs that the MsSql data-store factory and typed config provider were registered.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="dataStoreTypeName">The data-store type name that was registered.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11037,
        Level = LogLevel.Information,
        Message = "MsSql data-store type '{dataStoreTypeName}' registered factory and typed config provider")]
    public static partial IGenericMessage RegistrationCompleted(ILogger logger, string dataStoreTypeName);

    /// <summary>
    /// Logs that the MsSql data-store factory registration returned a failure result.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="dataStoreTypeName">The data-store type name whose factory registration failed.</param>
    /// <param name="message">The failure message from the registration result.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 61002,
        Level = LogLevel.Warning,
        Message = "MsSql data-store type '{dataStoreTypeName}' factory registration returned failure: {message}")]
    public static partial IGenericMessage FactoryRegistrationFailed(ILogger logger, string dataStoreTypeName, string? message);
}
