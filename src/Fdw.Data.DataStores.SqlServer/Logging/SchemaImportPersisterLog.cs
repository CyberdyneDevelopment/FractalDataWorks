using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Data.DataStores.SqlServer.Logging;

/// <summary>
/// MessageLogging for MsSqlSchemaImportPersister operations.
/// EventId range: 4050-4069
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("SQLSERVER")]
public static partial class SchemaImportPersisterLog
{
    /// <summary>
    /// Logs when a data path fails to persist during schema import.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="pathName">The name of the path that failed.</param>
    /// <param name="error">The error message.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Warning,
        Message = "Failed to persist path {pathName}: {error}")]
    public static partial IGenericMessage PathPersistFailed(
        ILogger logger,
        string pathName,
        string? error);

    /// <summary>
    /// Logs when a DataStore is successfully persisted.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dataStoreName">The name of the DataStore.</param>
    /// <param name="dataStoreId">The ID assigned to the DataStore.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "Persisted DataStore {dataStoreName} with ID {dataStoreId}")]
    public static partial IGenericMessage DataStorePersisted(
        ILogger logger,
        string dataStoreName,
        Guid dataStoreId);

    /// <summary>
    /// Logs when a data path fails to sync during schema synchronization.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="pathName">The name of the path that failed.</param>
    /// <param name="error">The error message.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Warning,
        Message = "Failed to sync path {pathName}: {error}")]
    public static partial IGenericMessage PathSyncFailed(
        ILogger logger,
        string pathName,
        string? error);

    /// <summary>
    /// Logs when a DataStore sync completes with change summary.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dataStoreId">The DataStore ID that was synced.</param>
    /// <param name="totalChanges">The total number of changes applied.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Synced DataStore {dataStoreId}: {totalChanges} total changes")]
    public static partial IGenericMessage DataStoreSynced(
        ILogger logger,
        Guid dataStoreId,
        int totalChanges);

    /// <summary>
    /// Logs when a container fails to persist during schema import.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="containerName">The name of the container that failed.</param>
    /// <param name="error">The error message.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 71002,
        Level = LogLevel.Warning,
        Message = "Failed to persist container {containerName}: {error}")]
    public static partial IGenericMessage ContainerPersistFailed(
        ILogger logger,
        string containerName,
        string? error);

    /// <summary>
    /// Logs when a field fails to persist during schema import.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="fieldName">The name of the field that failed.</param>
    /// <param name="error">The error message.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 71003,
        Level = LogLevel.Warning,
        Message = "Failed to persist field {fieldName}: {error}")]
    public static partial IGenericMessage FieldPersistFailed(
        ILogger logger,
        string fieldName,
        string? error);

    /// <summary>
    /// Logs when a container fails to sync during schema synchronization.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="containerName">The name of the container that failed.</param>
    /// <param name="error">The error message.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 71004,
        Level = LogLevel.Warning,
        Message = "Failed to sync container {containerName}: {error}")]
    public static partial IGenericMessage ContainerSyncFailed(
        ILogger logger,
        string containerName,
        string? error);

    /// <summary>
    /// Logs when the parent connection fails to update after schema import.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionId">The connection ID that failed to update.</param>
    /// <param name="error">The error message.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 71005,
        Level = LogLevel.Warning,
        Message = "Failed to update connection {connectionId} after schema import: {error}")]
    public static partial IGenericMessage ConnectionUpdateFailed(
        ILogger logger,
        Guid connectionId,
        string? error);
}
