using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Data.Components.Logging;

/// <summary>
/// MessageLogging methods for DataStoreProvider operations.
/// Provider-specific messages with domain context baked into templates.
/// EventId range: 8940-8959
/// </summary>
[MessageLoggingTypeCode("DATACOMPONENTS")]
public static partial class DataStoreProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Load Operations (8940-8943)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading the data store list fails.</summary>
    [MessageLogging(EventId = 71050, Level = LogLevel.Warning,
        Message = "DataStoreProvider: Failed to load data store list")]
    public static partial IGenericMessage LoadFailed(
        ILogger logger);

    /// <summary>Logs when loading the data store list fails with exception.</summary>
    [MessageLogging(EventId = 71051, Level = LogLevel.Warning,
        Message = "DataStoreProvider: Failed to load data store list")]
    public static partial IGenericMessage LoadException(
        ILogger logger,
        Exception exception);

    /// <summary>Logs when loading data store detail fails.</summary>
    [MessageLogging(EventId = 71052, Level = LogLevel.Warning,
        Message = "DataStoreProvider: Failed to load data store detail for '{dataStoreName}'")]
    public static partial IGenericMessage DetailLoadFailed(
        ILogger logger,
        string dataStoreName);

    /// <summary>Logs when loading data store detail fails with exception.</summary>
    [MessageLogging(EventId = 71053, Level = LogLevel.Warning,
        Message = "DataStoreProvider: Failed to load data store detail")]
    public static partial IGenericMessage DetailLoadException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // CRUD Operations (8944-8951)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when creating a data store fails.</summary>
    [MessageLogging(EventId = 71054, Level = LogLevel.Warning,
        Message = "DataStoreProvider: Failed to create data store")]
    public static partial IGenericMessage CreateFailed(
        ILogger logger);

    /// <summary>Logs when creating a data store fails with exception.</summary>
    [MessageLogging(EventId = 71055, Level = LogLevel.Warning,
        Message = "DataStoreProvider: Failed to create data store")]
    public static partial IGenericMessage CreateException(
        ILogger logger,
        Exception exception);

    /// <summary>Logs when updating a data store fails.</summary>
    [MessageLogging(EventId = 71056, Level = LogLevel.Warning,
        Message = "DataStoreProvider: Failed to update data store '{dataStoreName}'")]
    public static partial IGenericMessage UpdateFailed(
        ILogger logger,
        string dataStoreName);

    /// <summary>Logs when updating a data store fails with exception.</summary>
    [MessageLogging(EventId = 71057, Level = LogLevel.Warning,
        Message = "DataStoreProvider: Failed to update data store")]
    public static partial IGenericMessage UpdateException(
        ILogger logger,
        Exception exception);

    /// <summary>Logs when deleting a data store fails.</summary>
    [MessageLogging(EventId = 71058, Level = LogLevel.Warning,
        Message = "DataStoreProvider: Failed to delete data store '{dataStoreName}'")]
    public static partial IGenericMessage DeleteFailed(
        ILogger logger,
        string dataStoreName);

    /// <summary>Logs when deleting a data store fails with exception.</summary>
    [MessageLogging(EventId = 71059, Level = LogLevel.Warning,
        Message = "DataStoreProvider: Failed to delete data store")]
    public static partial IGenericMessage DeleteException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Discovery Operations (8950-8951)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when container discovery fails.</summary>
    [MessageLogging(EventId = 71060, Level = LogLevel.Warning,
        Message = "DataStoreProvider: Container discovery failed")]
    public static partial IGenericMessage DiscoverContainersFailed(
        ILogger logger);

    /// <summary>Logs when container discovery fails with exception.</summary>
    [MessageLogging(EventId = 71061, Level = LogLevel.Warning,
        Message = "DataStoreProvider: Container discovery failed")]
    public static partial IGenericMessage DiscoverContainersException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // ClientsDataStoreConfigurationProvider — .Clients-fetched IServiceConfigurationProvider
    // (91057-91066; first free ids after the DATACOMPONENTS typecode's 91025-91056 block)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Trace: entering Get(name).</summary>
    [MessageLogging(EventId = 91057, Level = LogLevel.Trace,
        Message = "ClientsDataStoreConfigurationProvider: Get(name) entry for '{dataStoreName}'")]
    public static partial IGenericMessage TraceGetByNameEntry(
        ILogger logger,
        string dataStoreName);

    /// <summary>Information: a DataStore detail was fetched and mapped to configuration.</summary>
    [MessageLogging(EventId = 91058, Level = LogLevel.Information,
        Message = "ClientsDataStoreConfigurationProvider: Mapped DataStore '{dataStoreName}' with {pathCount} path(s)")]
    public static partial IGenericMessage StoreMapped(
        ILogger logger,
        string dataStoreName,
        int pathCount);

    /// <summary>Trace: entering Get(id).</summary>
    [MessageLogging(EventId = 91059, Level = LogLevel.Trace,
        Message = "ClientsDataStoreConfigurationProvider: Get(id) entry for '{dataStoreId}'")]
    public static partial IGenericMessage TraceGetByIdEntry(
        ILogger logger,
        Guid dataStoreId);

    /// <summary>Warning: no DataStore summary matched the requested id.</summary>
    [MessageLogging(EventId = 91060, Level = LogLevel.Warning,
        Message = "ClientsDataStoreConfigurationProvider: No DataStore found for id '{dataStoreId}'")]
    public static partial IGenericMessage StoreByIdNotFound(
        ILogger logger,
        Guid dataStoreId);

    /// <summary>Trace: entering Get() (all DataStores).</summary>
    [MessageLogging(EventId = 91061, Level = LogLevel.Trace,
        Message = "ClientsDataStoreConfigurationProvider: Get() entry")]
    public static partial IGenericMessage TraceGetAllEntry(
        ILogger logger);

    /// <summary>Information: the DataStore summary list was fetched and mapped to shallow configurations.</summary>
    [MessageLogging(EventId = 91062, Level = LogLevel.Information,
        Message = "ClientsDataStoreConfigurationProvider: Mapped {dataStoreCount} DataStore summary(ies)")]
    public static partial IGenericMessage AllStoresMapped(
        ILogger logger,
        int dataStoreCount);

    /// <summary>Warning: the API client reported success but returned no DataStore body.</summary>
    [MessageLogging(EventId = 91063, Level = LogLevel.Warning,
        Message = "ClientsDataStoreConfigurationProvider: DataStoreApiClient returned success with no body for '{dataStoreName}'")]
    public static partial IGenericMessage ClientReturnedNullStore(
        ILogger logger,
        string dataStoreName);

    /// <summary>Warning: Save was called on the read-only .Clients-fetched configuration provider.</summary>
    [MessageLogging(EventId = 91064, Level = LogLevel.Warning,
        Message = "ClientsDataStoreConfigurationProvider: Save is not supported for '{dataStoreName}' — use DataStoreApiClient.CreateDataStore/UpdateDataStore")]
    public static partial IGenericMessage SaveNotSupported(
        ILogger logger,
        string dataStoreName);

    /// <summary>Warning: Delete(id) was called on the read-only .Clients-fetched configuration provider.</summary>
    [MessageLogging(EventId = 91065, Level = LogLevel.Warning,
        Message = "ClientsDataStoreConfigurationProvider: Delete(id) is not supported for '{dataStoreId}' — use DataStoreApiClient.DeleteDataStore")]
    public static partial IGenericMessage DeleteByIdNotSupported(
        ILogger logger,
        Guid dataStoreId);

    /// <summary>Warning: Delete(name) was called on the read-only .Clients-fetched configuration provider.</summary>
    [MessageLogging(EventId = 91066, Level = LogLevel.Warning,
        Message = "ClientsDataStoreConfigurationProvider: Delete(name) is not supported for '{dataStoreName}' — use DataStoreApiClient.DeleteDataStore")]
    public static partial IGenericMessage DeleteByNameNotSupported(
        ILogger logger,
        string dataStoreName);

    /// <summary>
    /// Logs when a record handed to the type-erased Save is not this provider's configuration type.
    /// </summary>
    [MessageLogging(EventId = 91019, Level = LogLevel.Error, Message = "Cannot save '{actualType}': this provider handles '{expectedType}'")]
    public static partial IGenericMessage UntypedSaveTypeMismatch(ILogger logger, string expectedType, string actualType);
}
