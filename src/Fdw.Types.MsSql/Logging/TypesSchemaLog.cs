using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Types.MsSql.Logging;

/// <summary>
/// MessageLogging for Types schema initialization operations.
/// EventId range: 4400-4409
/// </summary>
[MessageLoggingTypeCode("TYPES")]
public static partial class TypesSchemaLog
{
    /// <summary>
    /// Logs when a connection string is not found, skipping initialization.
    /// </summary>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Warning,
        Message = "Connection string '{connectionStringName}' not found. Skipping types schema initialization")]
    public static partial IGenericMessage ConnectionStringNotFound(
        ILogger logger,
        string connectionStringName);

    /// <summary>
    /// Logs when types schema initialization begins using a named connection string.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "Initializing types schema using connection string '{connectionStringName}'")]
    public static partial IGenericMessage InitializingWithConnectionString(
        ILogger logger,
        string connectionStringName);

    /// <summary>
    /// Logs when types schema initialization begins using a direct connection string.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Trace,
        Message = "Initializing types schema")]
    public static partial IGenericMessage Initializing(
        ILogger logger);

    /// <summary>
    /// Logs when types schema deployment fails.
    /// </summary>
    [MessageLogging(
        EventId = 71005,
        Level = LogLevel.Error,
        Message = "Failed to ensure types schema: {messages}")]
    public static partial IGenericMessage SchemaDeploymentFailed(
        ILogger logger,
        string messages);
}
