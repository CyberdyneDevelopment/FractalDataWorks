using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Data.Components.Logging;

/// <summary>
/// MessageLogging methods for DataStoreDetailProvider operations.
/// Domain-specific messages for the DataStore drill-down provider.
/// EventId range: 4660-4680
/// </summary>
[MessageLoggingTypeCode("DATACOMPONENTS")]
public static partial class DataStoreDetailProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Trace (4660-4662)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when the provider begins loading a DataStore.</summary>
    [MessageLogging(EventId = 11052, Level = LogLevel.Trace,
        Message = "DataStoreDetailProvider: Loading DataStore '{dataStoreName}'")]
    public static partial IGenericMessage LoadingDataStore(
        ILogger logger,
        string dataStoreName);

    /// <summary>Logs when the provider is building the drill-down tree.</summary>
    [MessageLogging(EventId = 11053, Level = LogLevel.Trace,
        Message = "DataStoreDetailProvider: Building drill-down tree for '{dataStoreName}'")]
    public static partial IGenericMessage BuildingTree(
        ILogger logger,
        string dataStoreName);

    /// <summary>Logs when the node selection changes in the detail view.</summary>
    [MessageLogging(EventId = 11054, Level = LogLevel.Trace,
        Message = "DataStoreDetailProvider: Selection changed to '{nodeType}' — '{nodeLabel}'")]
    public static partial IGenericMessage SelectionChanged(
        ILogger logger,
        string nodeType,
        string nodeLabel);

    // ═══════════════════════════════════════════════════════════════════════════
    // Debug (4663-4664)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs DataStore loaded with path and container counts.</summary>
    [MessageLogging(EventId = 11055, Level = LogLevel.Debug,
        Message = "DataStoreDetailProvider: DataStore '{dataStoreName}' loaded — {pathCount} paths, {containerCount} containers")]
    public static partial IGenericMessage DataStoreLoaded(
        ILogger logger,
        string dataStoreName,
        int pathCount,
        int containerCount);

    /// <summary>Logs when the tree has been mapped from the DataStore hierarchy.</summary>
    [MessageLogging(EventId = 11056, Level = LogLevel.Debug,
        Message = "DataStoreDetailProvider: Tree mapped for '{dataStoreName}'")]
    public static partial IGenericMessage TreeMapped(
        ILogger logger,
        string dataStoreName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Info (4665-4666)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when schema import completes successfully.</summary>
    [MessageLogging(EventId = 11057, Level = LogLevel.Information,
        Message = "DataStoreDetailProvider: Schema imported for connection '{connectionName}'")]
    public static partial IGenericMessage SchemaImported(
        ILogger logger,
        string connectionName);

    /// <summary>Logs when schema sync completes successfully.</summary>
    [MessageLogging(EventId = 11058, Level = LogLevel.Information,
        Message = "DataStoreDetailProvider: Schema synced for connection '{connectionName}'")]
    public static partial IGenericMessage SchemaSynced(
        ILogger logger,
        string connectionName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Warn (4667-4668)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when the requested DataStore was not found.</summary>
    [MessageLogging(EventId = 31002, Level = LogLevel.Warning,
        Message = "DataStoreDetailProvider: DataStore '{dataStoreName}' not found")]
    public static partial IGenericMessage DataStoreNotFound(
        ILogger logger,
        string dataStoreName);

    /// <summary>Logs when a DataStore has no paths.</summary>
    [MessageLogging(EventId = 31003, Level = LogLevel.Warning,
        Message = "DataStoreDetailProvider: DataStore '{dataStoreName}' has no paths")]
    public static partial IGenericMessage EmptyPaths(
        ILogger logger,
        string dataStoreName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error (4669-4670)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading a DataStore fails.</summary>
    [MessageLogging(EventId = 71047, Level = LogLevel.Error,
        Message = "DataStoreDetailProvider: Failed to load DataStore '{dataStoreName}'")]
    public static partial IGenericMessage LoadFailed(
        ILogger logger,
        Exception exception,
        string dataStoreName);

    /// <summary>Logs when schema import fails.</summary>
    [MessageLogging(EventId = 71048, Level = LogLevel.Error,
        Message = "DataStoreDetailProvider: Schema import failed for connection '{connectionName}'")]
    public static partial IGenericMessage ImportSchemaFailed(
        ILogger logger,
        Exception exception,
        string connectionName);

    /// <summary>Logs when a container is successfully added to a path.</summary>
    [MessageLogging(EventId = 11059, Level = LogLevel.Information,
        Message = "DataStoreDetailProvider: Added container '{containerName}' to path '{pathName}' in DataStore '{dataStoreName}'")]
    public static partial IGenericMessage ContainerAdded(
        ILogger logger,
        string containerName,
        string pathName,
        string dataStoreName);

    /// <summary>Logs when adding a container fails.</summary>
    [MessageLogging(EventId = 71049, Level = LogLevel.Error,
        Message = "DataStoreDetailProvider: Failed to add container '{containerName}' to path '{pathName}': {message}")]
    public static partial IGenericMessage AddContainerFailed(
        ILogger logger,
        string containerName,
        string pathName,
        string message);
}
