using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// Source-generated logging methods for DataStoreProvider.
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataStoreProviderLog
{
    /// <summary>Logs when a DataStore is retrieved from the registry.</summary>
    [MessageLogging(EventId = 11135, Level = LogLevel.Debug, Message = "Retrieved DataStore '{dataStoreName}' from registry")]
    public static partial IGenericMessage DataStoreRetrieved(ILogger logger, string dataStoreName);

    /// <summary>Logs when all DataStores are retrieved from the registry.</summary>
    [MessageLogging(EventId = 11136, Level = LogLevel.Information, Message = "Retrieved {count} DataStores from registry")]
    public static partial IGenericMessage AllDataStoresRetrieved(ILogger logger, int count);

    /// <summary>Logs when a DataStore is registered.</summary>
    [MessageLogging(EventId = 11137, Level = LogLevel.Information, Message = "Registered DataStore '{dataStoreId}' with type '{storeType}'")]
    public static partial IGenericMessage DataStoreRegistered(ILogger logger, string dataStoreId, string storeType);

    /// <summary>Logs when a DataStore is unregistered.</summary>
    [MessageLogging(EventId = 11138, Level = LogLevel.Information, Message = "Unregistered DataStore '{dataStoreName}'")]
    public static partial IGenericMessage DataStoreUnregistered(ILogger logger, string dataStoreName);

    /// <summary>Logs when path discovery starts for a DataStore.</summary>
    [MessageLogging(EventId = 11139, Level = LogLevel.Information, Message = "Discovering paths for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage DiscoveringPaths(ILogger logger, string dataStoreName);

    /// <summary>Logs when paths are discovered for a DataStore.</summary>
    [MessageLogging(EventId = 11140, Level = LogLevel.Information, Message = "Discovered {pathCount} paths for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage PathsDiscovered(ILogger logger, string dataStoreName, int pathCount);

    /// <summary>Logs when path discovery fails for a DataStore.</summary>
    [MessageLogging(EventId = 71026, Level = LogLevel.Error, Message = "Path discovery failed for DataStore '{dataStoreName}': {error}")]
    public static partial IGenericMessage PathDiscoveryFailed(ILogger logger, string dataStoreName, string error);

    /// <summary>Logs when container creation starts.</summary>
    [MessageLogging(EventId = 11141, Level = LogLevel.Information, Message = "Creating container for DataStore '{dataStoreId}' at path '{path}'")]
    public static partial IGenericMessage CreatingContainer(ILogger logger, string dataStoreId, string path);

    /// <summary>Logs when container creation fails.</summary>
    [MessageLogging(EventId = 91017, Level = LogLevel.Error, Message = "Failed to create container for DataStore '{dataStoreId}' at path '{path}': {error}")]
    public static partial IGenericMessage ContainerCreationFailed(ILogger logger, string dataStoreId, string path, string error);

    /// <summary>Logs when schema discovery starts.</summary>
    [MessageLogging(EventId = 11142, Level = LogLevel.Information, Message = "Discovering schema for DataStore '{dataStoreId}' at path '{path}'")]
    public static partial IGenericMessage DiscoveringSchema(ILogger logger, string dataStoreId, string path);

    /// <summary>Logs when connection testing starts.</summary>
    [MessageLogging(EventId = 11143, Level = LogLevel.Information, Message = "Testing connection to DataStore '{dataStoreName}'")]
    public static partial IGenericMessage TestingConnection(ILogger logger, string dataStoreName);

    /// <summary>Logs when connection test succeeds.</summary>
    [MessageLogging(EventId = 11144, Level = LogLevel.Information, Message = "Connection test succeeded for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage ConnectionTestSucceeded(ILogger logger, string dataStoreName);

    /// <summary>Logs when connection test fails.</summary>
    [MessageLogging(EventId = 71027, Level = LogLevel.Error, Message = "Connection test failed for DataStore '{dataStoreName}': {error}")]
    public static partial IGenericMessage ConnectionTestFailed(ILogger logger, string dataStoreName, string error);

    /// <summary>Returns a message indicating the DataStore is null.</summary>
    [MessageLogging(EventId = 21004, Level = LogLevel.Error, Message = "DataStore cannot be null")]
    public static partial IGenericMessage DataStoreNull(ILogger logger);

    /// <summary>Returns a message indicating the DataPath is null.</summary>
    [MessageLogging(EventId = 21005, Level = LogLevel.Error, Message = "DataPath cannot be null")]
    public static partial IGenericMessage DataPathNull(ILogger logger);

    /// <summary>Returns a message indicating schema discovery is not implemented for the store type.</summary>
    [MessageLogging(EventId = 91018, Level = LogLevel.Warning, Message = "Schema discovery for store type '{storeType}' is not yet implemented")]
    public static partial IGenericMessage SchemaDiscoveryNotImplemented(ILogger logger, string storeType);

    /// <summary>Returns a message indicating schema discovery failed.</summary>
    [MessageLogging(EventId = 71028, Level = LogLevel.Error, Message = "Schema discovery failed for DataStore '{dataStoreId}' at path '{dataPath}': {error}")]
    public static partial IGenericMessage SchemaDiscoveryError(ILogger logger, string dataStoreId, string dataPath, string error);

    /// <summary>Logs when a container is not found.</summary>
    [MessageLogging(EventId = 31030, Level = LogLevel.Warning, Message = "Container '{containerName}' not found")]
    public static partial IGenericMessage ContainerNotFound(ILogger logger, string containerName);

    /// <summary>Logs when a factory is registered for a store type.</summary>
    [MessageLogging(EventId = 11145, Level = LogLevel.Information, Message = "Registered factory for store type '{storeType}'")]
    public static partial IGenericMessage FactoryRegistered(ILogger logger, string storeType);

    /// <summary>Logs when a configuration provider is registered for a store type.</summary>
    [MessageLogging(EventId = 11146, Level = LogLevel.Information, Message = "Registered configuration provider for store type '{storeType}'")]
    public static partial IGenericMessage ConfigProviderRegistered(ILogger logger, string storeType);

    /// <summary>Logs when no factory is registered for a store type.</summary>
    [MessageLogging(EventId = 61015, Level = LogLevel.Warning, Message = "No factory registered for store type '{storeType}'")]
    public static partial IGenericMessage NoFactoryRegistered(ILogger logger, string storeType);

    /// <summary>Logs when schema discovery fails (method not supported in this path).</summary>
    [MessageLogging(EventId = 71029, Level = LogLevel.Error, Message = "Schema discovery failed for DataStore '{dataStoreName}': {errorMessage}")]
    public static partial IGenericMessage DiscoveryFailed(ILogger logger, string dataStoreName, string errorMessage);

    /// <summary>Returns a message indicating a DataStore was retrieved by ID.</summary>
    [MessageLogging(EventId = 11148, Level = LogLevel.Debug, Message = "DataStore retrieved by ID '{id}' from {source}")]
    public static partial IGenericMessage DataStoreRetrievedById(ILogger logger, Guid id, string source);

    /// <summary>Returns a message indicating a DataStore was not found by ID.</summary>
    [MessageLogging(EventId = 31031, Level = LogLevel.Warning, Message = "DataStore with ID '{id}' not found")]
    public static partial IGenericMessage DataStoreByIdNotFound(ILogger logger, Guid id);

    /// <summary>Returns a message indicating a container was not found by ID.</summary>
    [MessageLogging(EventId = 31032, Level = LogLevel.Warning, Message = "Container with ID '{containerId}' not found")]
    public static partial IGenericMessage ContainerByIdNotFound(ILogger logger, Guid containerId);

    /// <summary>
    /// Logs (endpoint-lookup altitude) when no DataStoreType is found while building a single container
    /// on demand for a request.
    /// </summary>
    /// <remarks>
    /// Why Error: the on-demand container build returns a Failure result the endpoint surfaces — the
    /// operation failed for this request. Distinct from <see cref="NoDataStoreTypeFoundAtStartup"/>,
    /// which is the startup-tree-load altitude where the whole store is silently dropped (Critical).
    /// </remarks>
    [MessageLogging(EventId = 61016, Level = LogLevel.Error, Message = "No DataStoreType '{storeType}' found for building container '{containerName}' from configuration")]
    public static partial IGenericMessage NoDataStoreTypeFoundForContainer(ILogger logger, string storeType, string containerName);

    /// <summary>
    /// Logs (startup tree-load altitude) when no DataStoreType is found for a store while building the
    /// runtime IDataStore tree, so the entire store is dropped from the tree.
    /// </summary>
    /// <remarks>
    /// Why Critical: this fires during startup tree assembly (BuildStoreViaBuilder). A missing transport
    /// type silently drops the whole store — every later container/endpoint targeting it then fails with
    /// "DataStore not found". That is an unrecoverable configuration defect surfaced at boot, not a
    /// per-request miss, so it logs at a higher altitude than the endpoint-lookup variant.
    /// </remarks>
    [MessageLogging(EventId = 61017, Level = LogLevel.Critical, Message = "No DataStoreType '{storeType}' found for DataStore '{dataStoreName}' during startup tree build — store dropped from the tree; every container targeting it will fail")]
    public static partial IGenericMessage NoDataStoreTypeFoundAtStartup(ILogger logger, string storeType, string dataStoreName);

    /// <summary>Logs when a DataStore configuration is missing ServiceOptionType.</summary>
    [MessageLogging(EventId = 61018, Level = LogLevel.Error, Message = "DataStore '{dataStoreName}' has no ServiceOptionType configured. Cannot determine store type.")]
    public static partial IGenericMessage DataStoreMissingServiceOptionType(ILogger logger, string dataStoreName);

    /// <summary>Logs when a container is built from configuration.</summary>
    [MessageLogging(EventId = 11149, Level = LogLevel.Information, Message = "Container '{containerName}' built from configuration for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage ContainerBuiltFromConfiguration(ILogger logger, string containerName, string dataStoreName);

    // ============================================================
    // Container Lookup Diagnostics (Trace/Debug)
    // ============================================================

    /// <summary>Logs container lookup request at debug level.</summary>
    [MessageLogging(EventId = 11150, Level = LogLevel.Debug, Message = "GetContainer called: DataStore='{dataStoreName}', Container='{containerName}'")]
    public static partial IGenericMessage GetContainerRequest(ILogger logger, string dataStoreName, string containerName);

    /// <summary>Logs cache contents summary at trace level.</summary>
    [MessageLogging(EventId = 11151, Level = LogLevel.Trace, Message = "Cache contents: {dataStoreCount} DataStores, {pathCount} DataPaths, {containerCount} DataContainers, {fieldCount} Fields")]
    public static partial IGenericMessage OptionsMonitorSummary(ILogger logger, int dataStoreCount, int pathCount, int containerCount, int fieldCount);

    /// <summary>Logs available DataStore names at trace level.</summary>
    [MessageLogging(EventId = 11152, Level = LogLevel.Trace, Message = "Available DataStores: [{dataStoreNames}]")]
    public static partial IGenericMessage AvailableDataStores(ILogger logger, string dataStoreNames);

    /// <summary>Logs when DataStore is found by name at debug level.</summary>
    [MessageLogging(EventId = 11153, Level = LogLevel.Debug, Message = "DataStore '{dataStoreName}' found with Id={dataStoreId}")]
    public static partial IGenericMessage DataStoreFoundByName(ILogger logger, string dataStoreName, Guid dataStoreId);

    /// <summary>Logs when DataStore is not found by name at debug level.</summary>
    [MessageLogging(EventId = 11154, Level = LogLevel.Debug, Message = "DataStore '{dataStoreName}' not found in cache, falling back to unscoped lookup")]
    public static partial IGenericMessage DataStoreNotFoundFallback(ILogger logger, string dataStoreName);

    /// <summary>Logs paths found for DataStore at trace level.</summary>
    [MessageLogging(EventId = 11155, Level = LogLevel.Trace, Message = "DataStore '{dataStoreName}' has {pathCount} paths: [{pathNames}]")]
    public static partial IGenericMessage DataStorePathsFound(ILogger logger, string dataStoreName, int pathCount, string pathNames);

    /// <summary>Logs container search in path at trace level.</summary>
    [MessageLogging(EventId = 11156, Level = LogLevel.Trace, Message = "Searching for container '{containerName}' in path '{pathName}' (PathId={pathId})")]
    public static partial IGenericMessage SearchingContainerInPath(ILogger logger, string containerName, string pathName, Guid pathId);

    /// <summary>Logs available containers in path at trace level.</summary>
    [MessageLogging(EventId = 11157, Level = LogLevel.Trace, Message = "Path '{pathName}' has containers: [{containerNames}]")]
    public static partial IGenericMessage PathContainers(ILogger logger, string pathName, string containerNames);

    /// <summary>Logs container found at debug level.</summary>
    [MessageLogging(EventId = 11158, Level = LogLevel.Debug, Message = "Container '{containerName}' found in path '{pathName}' with Id={containerId}")]
    public static partial IGenericMessage ContainerFoundInPath(ILogger logger, string containerName, string pathName, Guid containerId);

    /// <summary>Logs unscoped container lookup at debug level.</summary>
    [MessageLogging(EventId = 11159, Level = LogLevel.Debug, Message = "Unscoped GetContainer called for '{containerName}'")]
    public static partial IGenericMessage UnscopedGetContainer(ILogger logger, string containerName);

    /// <summary>Logs all available containers at trace level.</summary>
    [MessageLogging(EventId = 11160, Level = LogLevel.Trace, Message = "All available containers: [{containerNames}]")]
    public static partial IGenericMessage AllAvailableContainers(ILogger logger, string containerNames);

    // ============================================================
    // Diagnostic Logger Methods (Optional Logger Parameter)
    // ============================================================

    /// <summary>Logs the start of a container lookup at info level.</summary>
    [MessageLogging(EventId = 11161, Level = LogLevel.Information, Message = "[DIAGNOSTIC] {method} started: Container='{containerName}', DataStore='{dataStoreName}', Path='{pathName}'")]
    public static partial IGenericMessage DiagnosticLookupStart(ILogger logger, string method, string containerName, string? dataStoreName, string? pathName);

    /// <summary>Logs cache summary at debug level.</summary>
    [MessageLogging(EventId = 11162, Level = LogLevel.Debug, Message = "[DIAGNOSTIC] Cache loaded: {dataStoreCount} DataStores, {pathCount} Paths, {containerCount} Containers, {fieldCount} Fields")]
    public static partial IGenericMessage DiagnosticOptionsSummary(ILogger logger, int dataStoreCount, int pathCount, int containerCount, int fieldCount);

    /// <summary>Logs available DataStore names at trace level.</summary>
    [MessageLogging(EventId = 11163, Level = LogLevel.Trace, Message = "[DIAGNOSTIC] Available DataStores: [{dataStoreNames}]")]
    public static partial IGenericMessage DiagnosticAvailableDataStores(ILogger logger, string dataStoreNames);

    /// <summary>Logs available container names at trace level.</summary>
    [MessageLogging(EventId = 11164, Level = LogLevel.Trace, Message = "[DIAGNOSTIC] Available containers: [{containerNames}]")]
    public static partial IGenericMessage DiagnosticAvailableContainers(ILogger logger, string containerNames);

    /// <summary>Logs searching for DataStore at debug level.</summary>
    [MessageLogging(EventId = 11165, Level = LogLevel.Debug, Message = "[DIAGNOSTIC] Searching for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage DiagnosticSearchingDataStore(ILogger logger, string dataStoreName);

    /// <summary>Logs DataStore found at debug level.</summary>
    [MessageLogging(EventId = 11166, Level = LogLevel.Debug, Message = "[DIAGNOSTIC] DataStore found: '{dataStoreName}' (Id={dataStoreId})")]
    public static partial IGenericMessage DiagnosticDataStoreFound(ILogger logger, string dataStoreName, Guid dataStoreId);

    /// <summary>Logs DataStore not found, falling back at debug level.</summary>
    [MessageLogging(EventId = 11167, Level = LogLevel.Debug, Message = "[DIAGNOSTIC] DataStore '{dataStoreName}' not found, falling back to unscoped lookup")]
    public static partial IGenericMessage DiagnosticDataStoreNotFoundFallback(ILogger logger, string dataStoreName);

    /// <summary>Logs DataStore not found as error at debug level.</summary>
    [MessageLogging(EventId = 11168, Level = LogLevel.Debug, Message = "[DIAGNOSTIC] DataStore '{dataStoreName}' not found - returning error")]
    public static partial IGenericMessage DiagnosticDataStoreNotFoundError(ILogger logger, string dataStoreName);

    /// <summary>Logs DataStore not found by ID at debug level.</summary>
    [MessageLogging(EventId = 11169, Level = LogLevel.Debug, Message = "[DIAGNOSTIC] DataStore with Id={dataStoreId} not found")]
    public static partial IGenericMessage DiagnosticDataStoreNotFound(ILogger logger, Guid dataStoreId);

    /// <summary>Logs paths found for DataStore at trace level.</summary>
    [MessageLogging(EventId = 11170, Level = LogLevel.Trace, Message = "[DIAGNOSTIC] DataStore '{dataStoreName}' has {pathCount} paths: [{pathInfo}]")]
    public static partial IGenericMessage DiagnosticPathsForDataStore(ILogger logger, string dataStoreName, int pathCount, string pathInfo);

    /// <summary>Logs searching for path at debug level.</summary>
    [MessageLogging(EventId = 11171, Level = LogLevel.Debug, Message = "[DIAGNOSTIC] Searching for path '{pathName}' in DataStore '{dataStoreName}'")]
    public static partial IGenericMessage DiagnosticSearchingPath(ILogger logger, string pathName, string dataStoreName);

    /// <summary>Logs path found at debug level.</summary>
    [MessageLogging(EventId = 11172, Level = LogLevel.Debug, Message = "[DIAGNOSTIC] Path found: '{pathName}' (Id={pathId}, DataStoreId={dataStoreId})")]
    public static partial IGenericMessage DiagnosticPathFound(ILogger logger, string pathName, Guid pathId, Guid dataStoreId);

    /// <summary>Logs that a named path was not found in the resolved DataStore, listing the paths that ARE present.</summary>
    /// <remarks>Why: the path-not-found exit was previously silent, so a runtime datastore whose paths/containers
    /// didn't cascade-load failed with no trace of WHAT did load. This surfaces the gap at Warning.</remarks>
    [MessageLogging(EventId = 31033, Level = LogLevel.Warning, Message = "DataPath '{pathName}' not found in DataStore '{dataStoreName}' (which has {pathCount} path(s)): [{availablePaths}]")]
    public static partial IGenericMessage DataPathNotFoundInStore(ILogger logger, string pathName, string dataStoreName, int pathCount, string availablePaths);

    /// <summary>Logs path not found by ID at debug level.</summary>
    [MessageLogging(EventId = 11173, Level = LogLevel.Debug, Message = "[DIAGNOSTIC] Path with Id={pathId} not found")]
    public static partial IGenericMessage DiagnosticPathNotFound(ILogger logger, Guid pathId);

    /// <summary>Logs path not found in DataStore at debug level.</summary>
    [MessageLogging(EventId = 11174, Level = LogLevel.Debug, Message = "[DIAGNOSTIC] Path '{pathName}' not found in DataStore '{dataStoreName}'. Available paths: [{availablePaths}]")]
    public static partial IGenericMessage DiagnosticPathNotFoundInDataStore(ILogger logger, string pathName, string dataStoreName, string availablePaths);

    /// <summary>Logs containers in path at trace level.</summary>
    [MessageLogging(EventId = 11175, Level = LogLevel.Trace, Message = "[DIAGNOSTIC] Path '{pathName}' containers: [{containerNames}]")]
    public static partial IGenericMessage DiagnosticPathContainerList(ILogger logger, string pathName, string containerNames);

    /// <summary>Logs searching for container at debug level.</summary>
    [MessageLogging(EventId = 11176, Level = LogLevel.Debug, Message = "[DIAGNOSTIC] Searching for container '{containerName}' in path '{pathName}'")]
    public static partial IGenericMessage DiagnosticSearchingContainer(ILogger logger, string containerName, string pathName);

    /// <summary>Logs container config found at debug level.</summary>
    [MessageLogging(EventId = 11177, Level = LogLevel.Debug, Message = "[DIAGNOSTIC] Container config found: '{containerName}' (Id={containerId}, PathId={pathId})")]
    public static partial IGenericMessage DiagnosticContainerConfigFound(ILogger logger, string containerName, Guid containerId, Guid pathId);

    /// <summary>Logs container not found at debug level.</summary>
    [MessageLogging(EventId = 11178, Level = LogLevel.Debug, Message = "[DIAGNOSTIC] Container '{containerName}' not found in '{scopeName}'")]
    public static partial IGenericMessage DiagnosticContainerNotFound(ILogger logger, string containerName, string scopeName);

    /// <summary>Logs container not found in specific path at debug level.</summary>
    [MessageLogging(EventId = 11179, Level = LogLevel.Debug, Message = "[DIAGNOSTIC] Container '{containerName}' not found in path '{pathName}' of DataStore '{dataStoreName}'. Available: [{availableContainers}]")]
    public static partial IGenericMessage DiagnosticContainerNotFoundInPath(ILogger logger, string containerName, string pathName, string dataStoreName, string availableContainers);

    /// <summary>Logs fields found for container at trace level.</summary>
    [MessageLogging(EventId = 11180, Level = LogLevel.Trace, Message = "[DIAGNOSTIC] Container '{containerName}' has {fieldCount} fields")]
    public static partial IGenericMessage DiagnosticFieldsFound(ILogger logger, string containerName, int fieldCount);

    /// <summary>Logs building container at info level.</summary>
    [MessageLogging(EventId = 11181, Level = LogLevel.Information, Message = "[DIAGNOSTIC] Building container '{containerName}' from path '{pathName}' in DataStore '{dataStoreName}'")]
    public static partial IGenericMessage DiagnosticBuildingContainer(ILogger logger, string containerName, string pathName, string dataStoreName);

    // ============================================================
    // Trace Methods (5160-5169)
    // ============================================================

    /// <summary>Traces entry into DataStoreProvider.GetDataStore.</summary>
    [MessageLogging(EventId = 11182, Level = LogLevel.Trace, Message = "Entering DataStoreProvider.GetDataStore for '{dataStoreName}'")]
    public static partial IGenericMessage TraceGetDataStoreEntry(ILogger logger, string dataStoreName);

    /// <summary>Traces entry into DataStoreProvider.GetAllDataStores.</summary>
    [MessageLogging(EventId = 11183, Level = LogLevel.Trace, Message = "Entering DataStoreProvider.GetAllDataStores")]
    public static partial IGenericMessage TraceGetAllDataStoresEntry(ILogger logger);

    /// <summary>Traces entry into DataStoreProvider.RegisterDataStore.</summary>
    [MessageLogging(EventId = 11184, Level = LogLevel.Trace, Message = "Entering DataStoreProvider.RegisterDataStore for '{dataStoreId}'")]
    public static partial IGenericMessage TraceRegisterDataStoreEntry(ILogger logger, string dataStoreId);

    /// <summary>Traces entry into DataStoreProvider.GetDataPaths.</summary>
    [MessageLogging(EventId = 11185, Level = LogLevel.Trace, Message = "Entering DataStoreProvider.GetDataPaths for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage TraceGetDataPathsEntry(ILogger logger, string dataStoreName);

    /// <summary>Traces entry into DataStoreProvider.DiscoverPaths.</summary>
    [MessageLogging(EventId = 11186, Level = LogLevel.Trace, Message = "Entering DataStoreProvider.DiscoverPaths for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage TraceDiscoverPathsEntry(ILogger logger, string dataStoreName);

    /// <summary>Traces entry into DataStoreProvider.GetContainer.</summary>
    [MessageLogging(EventId = 11187, Level = LogLevel.Trace, Message = "Entering DataStoreProvider.GetContainer for DataStore '{dataStoreId}' at path '{pathName}'")]
    public static partial IGenericMessage TraceGetContainerEntry(ILogger logger, string dataStoreId, string pathName);

    /// <summary>Traces entry into DataStoreProvider.GetContainerByName.</summary>
    [MessageLogging(EventId = 11188, Level = LogLevel.Trace, Message = "Entering DataStoreProvider.GetContainerByName for '{containerName}'")]
    public static partial IGenericMessage TraceGetContainerByNameEntry(ILogger logger, string containerName);

    /// <summary>Traces entry into DataStoreProvider.DiscoverSchema.</summary>
    [MessageLogging(EventId = 11189, Level = LogLevel.Trace, Message = "Entering DataStoreProvider.DiscoverSchema for DataStore '{dataStoreId}'")]
    public static partial IGenericMessage TraceDiscoverSchemaEntry(ILogger logger, string dataStoreId);

    /// <summary>Traces entry into DataStoreProvider.GetDataStoreById.</summary>
    [MessageLogging(EventId = 11190, Level = LogLevel.Trace, Message = "Entering DataStoreProvider.GetDataStoreById for Id '{id}'")]
    public static partial IGenericMessage TraceGetDataStoreByIdEntry(ILogger logger, Guid id);

    /// <summary>Traces entry into DataStoreProvider.DiscoverDataStore by name.</summary>
    [MessageLogging(EventId = 11191, Level = LogLevel.Trace, Message = "Entering DataStoreProvider.DiscoverDataStore for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage TraceDiscoverDataStoreEntry(ILogger logger, string dataStoreName);

    // ============================================================
    // Unified tree builder (5170-5179)
    // ============================================================

    /// <summary>Logs the start of the unified IDataStore tree build.</summary>
    [MessageLogging(EventId = 11192, Level = LogLevel.Information, Message = "[DataStoreProvider] Unified IDataStore tree build started")]
    public static partial IGenericMessage TreeBuildStarted(ILogger logger);

    /// <summary>Logs the row counts gathered for the unified tree build.</summary>
    [MessageLogging(EventId = 11193, Level = LogLevel.Information, Message = "[DataStoreProvider] Tree build: {storeCount} stores, {pathCount} paths, {containerCount} containers, {fieldCount} cfg fields, {keyFieldCount} cfg key fields")]
    public static partial IGenericMessage TreeBuildCounts(ILogger logger, int storeCount, int pathCount, int containerCount, int fieldCount, int keyFieldCount);

    /// <summary>Logs the completion of the unified IDataStore tree build.</summary>
    [MessageLogging(EventId = 11194, Level = LogLevel.Information, Message = "[DataStoreProvider] Unified IDataStore tree built: {storeCount} stores")]
    public static partial IGenericMessage TreeBuildCompleted(ILogger logger, int storeCount);

    /// <summary>Logs when cfg DataStore enumeration fails (best-effort — ctrl-only tree will be used).</summary>
    [MessageLogging(EventId = 71030, Level = LogLevel.Warning, Message = "[DataStoreProvider] cfg DataStore enumeration failed: {error} — proceeding with ctrl-only stores")]
    public static partial IGenericMessage CfgStoreEnumerationFailed(ILogger logger, string error);


    // ============================================================
    // DataContainerBuilder (on-demand lazy build, 5176-5182)
    // ============================================================

    /// <summary>Logs when a container is successfully built on demand and added to cache.</summary>
    [MessageLogging(EventId = 11195, Level = LogLevel.Information, Message = "[DataContainerBuilder] Container built: '{qualifiedName}' fields={fieldCount} keys={keyCount}")]
    public static partial IGenericMessage ContainerBuilt(ILogger logger, string qualifiedName, int fieldCount, int keyCount);

    /// <summary>Logs when a container is returned from the on-demand cache.</summary>
    [MessageLogging(EventId = 11196, Level = LogLevel.Trace, Message = "[DataContainerBuilder] Cache hit: '{qualifiedName}'")]
    public static partial IGenericMessage ContainerCacheHit(ILogger logger, string qualifiedName);

    /// <summary>Logs when the DataPath gateway query fails during on-demand container build.</summary>
    [MessageLogging(EventId = 71031, Level = LogLevel.Error, Message = "[DataContainerBuilder] DataPath query failed for store='{storeName}' path='{pathName}': {error}")]
    public static partial IGenericMessage ContainerBuildPathQueryFailed(ILogger logger, string storeName, string pathName, string error);

    /// <summary>Logs when the DataPath row is not found during on-demand container build.</summary>
    [MessageLogging(EventId = 31034, Level = LogLevel.Error, Message = "[DataContainerBuilder] DataPath '{pathName}' not found in store '{storeName}'")]
    public static partial IGenericMessage ContainerBuildPathNotFound(ILogger logger, string storeName, string pathName);

    /// <summary>Logs when the DataContainer gateway query fails during on-demand container build.</summary>
    [MessageLogging(EventId = 71032, Level = LogLevel.Error, Message = "[DataContainerBuilder] DataContainer query failed for container='{containerName}' path='{pathName}': {error}")]
    public static partial IGenericMessage ContainerBuildContainerQueryFailed(ILogger logger, string containerName, string pathName, string error);

    /// <summary>Logs when the DataContainer row is not found during on-demand container build.</summary>
    [MessageLogging(EventId = 31035, Level = LogLevel.Error, Message = "[DataContainerBuilder] DataContainer '{containerName}' not found in path '{pathName}' of store '{storeName}'")]
    public static partial IGenericMessage ContainerBuildContainerNotFound(ILogger logger, string containerName, string pathName, string storeName);

    /// <summary>Logs when the DataContainerField gateway query fails during on-demand container build.</summary>
    [MessageLogging(EventId = 71033, Level = LogLevel.Error, Message = "[DataContainerBuilder] DataContainerField query failed for container='{containerName}': {error}")]
    public static partial IGenericMessage ContainerBuildFieldQueryFailed(ILogger logger, string containerName, string error);

    // ============================================================
    // Load (5183-5186)
    // ============================================================

    /// <summary>Traces the start of Load.</summary>
    [MessageLogging(EventId = 11197, Level = LogLevel.Trace, Message = "[Load] Starting Load")]
    public static partial IGenericMessage LoadStarted(ILogger logger);

    /// <summary>Logs successful completion of Load.</summary>
    [MessageLogging(EventId = 11198, Level = LogLevel.Information, Message = "[Load] Loaded {storeCount} DataStores, {pathCount} DataPaths, {containerCount} DataContainers")]
    public static partial IGenericMessage LoadCompleted(ILogger logger, int storeCount, int pathCount, int containerCount);

    /// <summary>Logs when Load fails to retrieve configurations from the provider.</summary>
    [MessageLogging(EventId = 71034, Level = LogLevel.Error, Message = "[Load] Failed to load DataStore configurations: {error}")]
    public static partial IGenericMessage LoadFailed(ILogger logger, Exception exception, string error);

    /// <summary>Logs when a DataStore is skipped during Load due to a missing name.</summary>
    [MessageLogging(EventId = 21007, Level = LogLevel.Warning, Message = "[Load] Skipping DataStore with empty name (Id={id})")]
    public static partial IGenericMessage LoadSkippedEmptyName(ILogger logger, Guid id);
}
