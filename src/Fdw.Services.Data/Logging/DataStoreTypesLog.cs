using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for DataStoreTypes operations.
/// EventId range: 5120-5155
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataStoreTypesLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Configure Phase (5120-5124)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that IOptions bindings have been configured.
    /// </summary>
    [MessageLogging(
        EventId = 11199,
        Level = LogLevel.Debug,
        Message = "[DataStoreTypes] Configured IOptions bindings")]
    public static partial IGenericMessage ConfiguredOptionsBindings(ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Register Phase (5125-5129)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that infrastructure services have been registered.
    /// </summary>
    [MessageLogging(
        EventId = 11200,
        Level = LogLevel.Debug,
        Message = "[DataStoreTypes] Registered infrastructure services")]
    public static partial IGenericMessage RegisteredInfrastructureServices(ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Initialize Phase - Summary (5130-5134)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs summary of IOptions-bound configuration counts.
    /// </summary>
    [MessageLogging(
        EventId = 11201,
        Level = LogLevel.Information,
        Message = "[DataStoreTypes] IOptions bound: {dataStoreCount} DataStores, {dataPathCount} DataPaths, {dataContainerCount} DataContainers, {fieldCount} Fields")]
    public static partial IGenericMessage OptionsBindingSummary(ILogger logger, int dataStoreCount, int dataPathCount, int dataContainerCount, int fieldCount);

    /// <summary>
    /// Logs DataStoreTypes initialization completion.
    /// </summary>
    [MessageLogging(
        EventId = 11202,
        Level = LogLevel.Information,
        Message = "[DataStoreTypes] Initialized {typeCount} DataStore types, {configCount} configured DataStores")]
    public static partial IGenericMessage DataStoreTypesInitialized(ILogger logger, int typeCount, int configCount);

    /// <summary>
    /// Logs the input list counts fed to DataStoreBuilder.Build.
    /// </summary>
    [MessageLogging(
        EventId = 11203,
        Level = LogLevel.Trace,
        Message = "[DataStoreBuilder] Inputs — DataStore:{dataStoreCount} DataPath:{dataPathCount} DataContainer:{dataContainerCount} Connection:{connectionCount} MsSqlDataContainer:{msSqlDataContainerCount}")]
    public static partial IGenericMessage TreeBuilderInputs(ILogger logger, int dataStoreCount, int dataPathCount, int dataContainerCount, int connectionCount, int msSqlDataContainerCount);

    /// <summary>
    /// Logs each DataStoreConfiguration as fed into DataStoreBuilder.Build (Id+Name+TypeId).
    /// </summary>
    [MessageLogging(
        EventId = 11204,
        Level = LogLevel.Trace,
        Message = "[DataStoreBuilder] DataStore Id={id} Name='{name}' TypeId='{typeId}'")]
    public static partial IGenericMessage TreeBuilderDataStoreInput(ILogger logger, Guid id, string name, string typeId);

    /// <summary>
    /// Logs each DataPath FK group and whether it matched a known DataStore.
    /// </summary>
    [MessageLogging(
        EventId = 11205,
        Level = LogLevel.Trace,
        Message = "[DataStoreBuilder] DataPath FK group DataStoreId={fk} pathCount={count} matchedStore='{matchedStoreName}'")]
    public static partial IGenericMessage TreeBuilderPathFkGroup(ILogger logger, Guid fk, int count, string matchedStoreName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Initialize Phase - Individual Items (5135-5139) - Debug level
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs an individual DataStore loaded from configuration.
    /// </summary>
    [MessageLogging(
        EventId = 11206,
        Level = LogLevel.Debug,
        Message = "[DataStoreTypes] DataStore: Id={id}, Name='{name}', Type='{serviceOptionType}'")]
    public static partial IGenericMessage DataStoreLoaded(ILogger logger, Guid id, string name, string serviceOptionType);

    /// <summary>
    /// Logs an individual DataPath loaded from configuration.
    /// </summary>
    [MessageLogging(
        EventId = 11207,
        Level = LogLevel.Debug,
        Message = "[DataStoreTypes] DataPath: Id={id}, Name='{name}', DataStoreId={dataStoreId}")]
    public static partial IGenericMessage DataPathLoaded(ILogger logger, Guid id, string name, Guid dataStoreId);

    /// <summary>
    /// Logs an individual DataContainer loaded from configuration.
    /// </summary>
    [MessageLogging(
        EventId = 11208,
        Level = LogLevel.Debug,
        Message = "[DataStoreTypes] DataContainer: Id={id}, Name='{name}', DataPathId={dataPathId}")]
    public static partial IGenericMessage DataContainerLoaded(ILogger logger, Guid id, string name, Guid dataPathId);

    /// <summary>
    /// Logs an individual DataContainerField loaded from configuration.
    /// </summary>
    // Why: IsPrimaryKey removed from DataContainerFieldConfiguration — PK identity is now in DataContainerKeyField entries.
    [MessageLogging(
        EventId = 11209,
        Level = LogLevel.Trace,
        Message = "[DataStoreTypes] Field: Id={id}, Name='{name}', DataContainerId={dataContainerId}, DataType='{dataType}'")]
    public static partial IGenericMessage FieldLoaded(ILogger logger, Guid id, string name, Guid dataContainerId, string dataType);

    // ═══════════════════════════════════════════════════════════════════════════
    // RegisterContainers Phase (5140-5149)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs an individual container registered with provider.
    /// </summary>
    [MessageLogging(
        EventId = 11210,
        Level = LogLevel.Debug,
        Message = "[DataStoreTypes] Registered container '{containerName}' for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage ContainerRegistered(ILogger logger, string containerName, string dataStoreName);

    /// <summary>
    /// Logs when container build fails.
    /// </summary>
    [MessageLogging(
        EventId = 91019,
        Level = LogLevel.Warning,
        Message = "[DataStoreTypes] Container build failed for '{containerName}': {error}")]
    public static partial IGenericMessage ContainerBuildFailed(ILogger logger, string containerName, string error);

    /// <summary>
    /// Logs summary of containers registered.
    /// </summary>
    [MessageLogging(
        EventId = 11211,
        Level = LogLevel.Information,
        Message = "[DataStoreTypes] Registered {count} containers")]
    public static partial IGenericMessage ContainersRegisteredSummary(ILogger logger, int count);

    /// <summary>
    /// Logs when no DataStoreType is found for a store type.
    /// </summary>
    [MessageLogging(
        EventId = 61019,
        Level = LogLevel.Warning,
        Message = "[DataStoreTypes] No DataStoreType found for '{storeType}'")]
    public static partial IGenericMessage NoDataStoreTypeFound(ILogger logger, string storeType);

    /// <summary>
    /// Logs when a DataStore configuration is missing ServiceOptionType during initialization.
    /// </summary>
    [MessageLogging(
        EventId = 61020,
        Level = LogLevel.Error,
        Message = "[DataStoreTypes] DataStore '{dataStoreName}' has no ServiceOptionType configured. Skipping.")]
    public static partial IGenericMessage DataStoreMissingServiceOptionType(ILogger logger, string dataStoreName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Hierarchy Diagnostics (5144-5149) - Trace level
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs DataStore hierarchy with paths at trace level.
    /// </summary>
    [MessageLogging(
        EventId = 11212,
        Level = LogLevel.Trace,
        Message = "[DataStoreTypes] Hierarchy: DataStore '{dataStoreName}' (Id={dataStoreId}) has paths: [{pathNames}]")]
    public static partial IGenericMessage DataStoreHierarchy(ILogger logger, string dataStoreName, Guid dataStoreId, string pathNames);

    /// <summary>
    /// Logs path hierarchy with containers at trace level.
    /// </summary>
    [MessageLogging(
        EventId = 11213,
        Level = LogLevel.Trace,
        Message = "[DataStoreTypes] Hierarchy: Path '{pathName}' (Id={pathId}) has containers: [{containerNames}]")]
    public static partial IGenericMessage PathHierarchy(ILogger logger, string pathName, Guid pathId, string containerNames);

    /// <summary>
    /// Logs the full qualified name for a container at trace level.
    /// </summary>
    [MessageLogging(
        EventId = 11214,
        Level = LogLevel.Trace,
        Message = "[DataStoreTypes] Container qualified name: {dataStoreName}.{pathName}.{containerName}")]
    public static partial IGenericMessage ContainerQualifiedName(ILogger logger, string dataStoreName, string pathName, string containerName);

    /// <summary>
    /// Logs available container names list at debug level.
    /// </summary>
    [MessageLogging(
        EventId = 11215,
        Level = LogLevel.Debug,
        Message = "[DataStoreTypes] Available containers by name: [{containerNames}]")]
    public static partial IGenericMessage AvailableContainerNames(ILogger logger, string containerNames);

    /// <summary>
    /// Logs available DataStore names list at debug level.
    /// </summary>
    [MessageLogging(
        EventId = 11216,
        Level = LogLevel.Debug,
        Message = "[DataStoreTypes] Available DataStores by name: [{dataStoreNames}]")]
    public static partial IGenericMessage AvailableDataStoreNames(ILogger logger, string dataStoreNames);

    // ═══════════════════════════════════════════════════════════════════════════
    // DataStoreType Configure/Register Phase (5150-5151)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that a data store type is being configured from a section.
    /// </summary>
    [MessageLogging(
        EventId = 11217,
        Level = LogLevel.Information,
        Message = "[DataStoreTypes] Configuring data store type '{name}' from section '{sectionName}'")]
    public static partial IGenericMessage ConfiguringDataStoreType(ILogger logger, string name, string sectionName);

    /// <summary>
    /// Logs that required services are being registered for a data store type.
    /// </summary>
    [MessageLogging(
        EventId = 11218,
        Level = LogLevel.Information,
        Message = "[DataStoreTypes] Registering required services for data store type '{name}'")]
    public static partial IGenericMessage RegisteringDataStoreTypeServices(ILogger logger, string name);

    // ═══════════════════════════════════════════════════════════════════════════
    // DataSetTypes Configure/Register/Initialize Phase (5152-5155)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that DataSet IOptions bindings have been configured.
    /// </summary>
    [MessageLogging(
        EventId = 11219,
        Level = LogLevel.Debug,
        Message = "[DataSetTypes] Configured DataSet IOptions bindings")]
    public static partial IGenericMessage ConfiguredDataSetOptionsBindings(ILogger logger);

    /// <summary>
    /// Logs that DataSet infrastructure services have been registered.
    /// </summary>
    [MessageLogging(
        EventId = 11220,
        Level = LogLevel.Debug,
        Message = "[DataSetTypes] Registered DataSet infrastructure services")]
    public static partial IGenericMessage RegisteredDataSetInfrastructure(ILogger logger);

    /// <summary>
    /// Logs the count of DataSet types being registered from TypeCollection.
    /// </summary>
    [MessageLogging(
        EventId = 11221,
        Level = LogLevel.Information,
        Message = "[DataSetTypes] Registering {count} DataSet types from TypeCollection")]
    public static partial IGenericMessage RegisteringDataSetTypes(ILogger logger, int count);

    /// <summary>
    /// Logs an individual DataSet registered with field counts.
    /// </summary>
    [MessageLogging(
        EventId = 11222,
        Level = LogLevel.Debug,
        Message = "[DataSetTypes] Registered DataSet '{name}' with {fieldCount} fields ({calculatedCount} calculated)")]
    public static partial IGenericMessage RegisteredDataSet(ILogger logger, string name, int fieldCount, int calculatedCount);

    // ═══════════════════════════════════════════════════════════════════════════
    // DataStoreType.Build failures (7000-7010)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when Build receives a container/path/fields configuration of an unexpected type.
    /// </summary>
    [MessageLogging(
        EventId = 91020,
        Level = LogLevel.Error,
        Message = "[DataStoreType] Build: unexpected configuration type — expected {expected}, got '{actualType}'")]
    public static partial IGenericMessage BuildUnexpectedConfigurationType(ILogger logger, string expected, string actualType);

    /// <summary>
    /// Logs when Build receives a source configuration of an unexpected type.
    /// </summary>
    [MessageLogging(
        EventId = 91021,
        Level = LogLevel.Error,
        Message = "[DataStoreType] Build: unexpected source configuration type — expected DataSetSourceConfiguration, got '{actualType}'")]
    public static partial IGenericMessage BuildUnexpectedSourceType(ILogger logger, string actualType);

    /// <summary>
    /// Logs when a source is missing a required field path / endpoint / table name.
    /// </summary>
    [MessageLogging(
        EventId = 21008,
        Level = LogLevel.Error,
        Message = "[DataStoreType] Build: source missing required field '{fieldName}'")]
    public static partial IGenericMessage BuildSourceMissingField(ILogger logger, string fieldName);

    /// <summary>
    /// Logs when a container build throws an unexpected exception.
    /// </summary>
    [MessageLogging(
        EventId = 91022,
        Level = LogLevel.Error,
        Message = "[DataStoreType] Build: exception while constructing container '{containerName}': {error}")]
    public static partial IGenericMessage BuildContainerException(ILogger logger, Exception ex, string containerName, string error);
}
