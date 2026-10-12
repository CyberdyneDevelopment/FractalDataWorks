using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Connections.MsSql.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.Logging;

/// <summary>
/// MessageLogging for MsSql schema commands.
/// EventId range: 5210-5249 (Schema Commands)
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlSchemaCommandsLog
{
    /// <summary>
    /// Logs when schema discovery starts.
    /// </summary>
    [MessageLogging(
        EventId = 11038,
        Level = LogLevel.Information,
        Message = "Starting schema discovery for database '{databaseName}'")]
    public static partial IGenericMessage SchemaDiscoveryStarted(ILogger logger, string databaseName);

    /// <summary>
    /// Logs when schema discovery completes.
    /// </summary>
    [MessageLogging(
        EventId = 11039,
        Level = LogLevel.Information,
        Message = "Schema discovery completed: {pathCount} paths, {containerCount} containers, {fieldCount} fields")]
    public static partial IGenericMessage SchemaDiscoveryCompleted(ILogger logger, int pathCount, int containerCount, int fieldCount);

    /// <summary>
    /// Logs when schema discovery fails.
    /// </summary>
    [MessageLogging(
        EventId = 71022,
        Level = LogLevel.Error,
        Message = "Schema discovery failed: {errorMessage}")]
    public static partial IGenericMessage SchemaDiscoveryFailed(ILogger logger, string errorMessage);

    /// <summary>
    /// Logs when schema discovery throws an exception.
    /// </summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Schema discovery exception")]
    public static partial IGenericMessage SchemaDiscoveryException(ILogger logger, Exception ex);

    /// <summary>
    /// Logs when connection is not open.
    /// </summary>
    [MessageLogging(
        EventId = 41005,
        Level = LogLevel.Error,
        Message = "Connection must be open before schema discovery")]
    public static partial IGenericMessage ConnectionNotOpen(ILogger logger);

    /// <summary>
    /// Logs when connection fails.
    /// </summary>
    [MessageLogging(
        EventId = 71023,
        Level = LogLevel.Error,
        Message = "Connection failed: {errorMessage}")]
    public static partial IGenericMessage ConnectionFailed(ILogger logger, string errorMessage);

    /// <summary>
    /// Logs when schema persistence starts.
    /// </summary>
    [MessageLogging(
        EventId = 11040,
        Level = LogLevel.Information,
        Message = "Starting schema persistence for connection '{connectionName}' ({containerCount} containers)")]
    public static partial IGenericMessage SchemaPersistStarted(ILogger logger, string connectionName, int containerCount);

    /// <summary>
    /// Logs when schema persistence completes.
    /// </summary>
    [MessageLogging(
        EventId = 11041,
        Level = LogLevel.Information,
        Message = "Schema persistence completed: DataStore '{dataStoreId}', {pathsAdded} paths, {containersAdded} containers, {fieldsAdded} fields added")]
    public static partial IGenericMessage SchemaPersistCompleted(ILogger logger, Guid dataStoreId, int pathsAdded, int containersAdded, int fieldsAdded);

    /// <summary>
    /// Logs when writer creation fails.
    /// </summary>
    [MessageLogging(
        EventId = 61003,
        Level = LogLevel.Error,
        Message = "Failed to create configuration writer for '{writerType}'")]
    public static partial IGenericMessage WriterCreationFailed(ILogger logger, string writerType);

    /// <summary>
    /// Logs when save operation fails.
    /// </summary>
    [MessageLogging(
        EventId = 71024,
        Level = LogLevel.Error,
        Message = "Failed to save '{entityType}': {errorMessage}")]
    public static partial IGenericMessage SaveFailed(ILogger logger, string entityType, string errorMessage);

    /// <summary>
    /// Logs when schema persistence throws an exception.
    /// </summary>
    [MessageLogging(
        EventId = 71025,
        Level = LogLevel.Error,
        Message = "Schema persistence exception")]
    public static partial IGenericMessage SchemaPersistException(ILogger logger, Exception ex);
}
