using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.Logging;

/// <summary>
/// MessageLogging for MsSql schema discovery operations.
/// EventId range: 8750-8799 (Schema Discovery - MsSql specific)
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlSchemaDiscoveryLog
{
    /// <summary>
    /// Logs when schema discovery starts for a DataStore.
    /// </summary>
    [MessageLogging(
        EventId = 11042,
        Level = LogLevel.Information,
        Message = "Starting schema discovery for database '{databaseName}'")]
    public static partial IGenericMessage SchemaDiscoveryStarted(ILogger logger, string databaseName);

    /// <summary>
    /// Logs when schemas have been discovered.
    /// </summary>
    [MessageLogging(
        EventId = 11043,
        Level = LogLevel.Information,
        Message = "Discovered {schemaCount} schemas")]
    public static partial IGenericMessage SchemasDiscovered(ILogger logger, int schemaCount);

    /// <summary>
    /// Logs when a container (table/view) is discovered.
    /// </summary>
    [MessageLogging(
        EventId = 11044,
        Level = LogLevel.Debug,
        Message = "Discovered {containerType} '{schemaName}.{objectName}' with {fieldCount} fields")]
    public static partial IGenericMessage ContainerDiscovered(ILogger logger, string containerType, string schemaName, string objectName, int fieldCount);

    /// <summary>
    /// Logs when schema discovery completes successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11045,
        Level = LogLevel.Information,
        Message = "Schema discovery completed: {pathCount} paths, {containerCount} containers, {fieldCount} fields")]
    public static partial IGenericMessage SchemaDiscoveryCompleted(ILogger logger, int pathCount, int containerCount, int fieldCount);

    /// <summary>
    /// Logs when schema discovery fails.
    /// </summary>
    [MessageLogging(
        EventId = 71026,
        Level = LogLevel.Error,
        Message = "Schema discovery failed: {errorMessage}")]
    public static partial IGenericMessage SchemaDiscoveryFailed(ILogger logger, string errorMessage);

    /// <summary>
    /// Logs when connection fails during discovery.
    /// </summary>
    [MessageLogging(
        EventId = 71027,
        Level = LogLevel.Error,
        Message = "Connection failed during discovery: {errorMessage}")]
    public static partial IGenericMessage ConnectionFailed(ILogger logger, string errorMessage);

    /// <summary>
    /// Logs when a container is not found.
    /// </summary>
    [MessageLogging(
        EventId = 31005,
        Level = LogLevel.Warning,
        Message = "Container '{schemaName}.{objectName}' not found in database")]
    public static partial IGenericMessage ContainerNotFound(ILogger logger, string schemaName, string objectName);

    /// <summary>
    /// Logs when container discovery starts.
    /// </summary>
    [MessageLogging(
        EventId = 11046,
        Level = LogLevel.Debug,
        Message = "Starting container discovery for '{schemaName}.{objectName}'")]
    public static partial IGenericMessage ContainerDiscoveryStarted(ILogger logger, string schemaName, string objectName);

    /// <summary>
    /// Logs when container discovery fails.
    /// </summary>
    [MessageLogging(
        EventId = 71028,
        Level = LogLevel.Error,
        Message = "Container discovery failed for '{schemaName}.{objectName}': {errorMessage}")]
    public static partial IGenericMessage ContainerDiscoveryFailed(ILogger logger, string schemaName, string objectName, string errorMessage);

    /// <summary>
    /// Logs a warning during schema discovery for a specific schema.
    /// </summary>
    [MessageLogging(
        EventId = 71029,
        Level = LogLevel.Warning,
        Message = "Warning during schema discovery for '{schemaName}': {warning}")]
    public static partial IGenericMessage SchemaDiscoveryWarning(ILogger logger, string schemaName, string warning);

    /// <summary>
    /// Logs when discovery is triggered for a DataStore.
    /// </summary>
    [MessageLogging(
        EventId = 11047,
        Level = LogLevel.Information,
        Message = "Auto-discovery triggered for DataStore '{dataStoreName}' - no containers found")]
    public static partial IGenericMessage DiscoveryTriggered(ILogger logger, string dataStoreName);

    /// <summary>
    /// Logs when containers are registered from discovery.
    /// </summary>
    [MessageLogging(
        EventId = 11048,
        Level = LogLevel.Information,
        Message = "Registered {containerCount} containers from discovery for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage ContainersRegisteredFromDiscovery(ILogger logger, int containerCount, string dataStoreName);

    /// <summary>
    /// Logs when a new container is added during schema refresh.
    /// </summary>
    [MessageLogging(
        EventId = 11049,
        Level = LogLevel.Information,
        Message = "New container discovered: '{schemaName}.{objectName}' ({containerType})")]
    public static partial IGenericMessage NewContainerDiscovered(ILogger logger, string schemaName, string objectName, string containerType);

    /// <summary>
    /// Logs when a column is added during schema refresh.
    /// </summary>
    [MessageLogging(
        EventId = 11050,
        Level = LogLevel.Information,
        Message = "New column discovered: '{schemaName}.{tableName}.{columnName}' ({sqlType})")]
    public static partial IGenericMessage NewColumnDiscovered(ILogger logger, string schemaName, string tableName, string columnName, string sqlType);

    /// <summary>
    /// Logs a warning when a container appears to have been removed.
    /// </summary>
    [MessageLogging(
        EventId = 41006,
        Level = LogLevel.Information,
        Message = "Container may have been removed: '{schemaName}.{objectName}' - was previously registered but not found in schema")]
    public static partial IGenericMessage ContainerRemovedWarning(ILogger logger, string schemaName, string objectName);

    /// <summary>
    /// Logs a warning when a column appears to have been removed.
    /// </summary>
    [MessageLogging(
        EventId = 41007,
        Level = LogLevel.Information,
        Message = "Column may have been removed: '{schemaName}.{tableName}.{columnName}' - was previously registered but not found in schema")]
    public static partial IGenericMessage ColumnRemovedWarning(ILogger logger, string schemaName, string tableName, string columnName);

    /// <summary>
    /// Logs a warning when a column type has changed.
    /// </summary>
    [MessageLogging(
        EventId = 41008,
        Level = LogLevel.Warning,
        Message = "Column type changed: '{schemaName}.{tableName}.{columnName}' from '{oldType}' to '{newType}'")]
    public static partial IGenericMessage ColumnTypeChangedWarning(ILogger logger, string schemaName, string tableName, string columnName, string oldType, string newType);

    /// <summary>
    /// Logs when DataStore is null.
    /// </summary>
    [MessageLogging(
        EventId = 21006,
        Level = LogLevel.Error,
        Message = "DataStore cannot be null")]
    public static partial IGenericMessage DataStoreNull(ILogger logger);

    /// <summary>
    /// Logs when schema name is null or empty.
    /// </summary>
    [MessageLogging(
        EventId = 21007,
        Level = LogLevel.Error,
        Message = "Schema name cannot be null or empty")]
    public static partial IGenericMessage SchemaNameRequired(ILogger logger);

    /// <summary>
    /// Logs when container name is null or empty.
    /// </summary>
    [MessageLogging(
        EventId = 21008,
        Level = LogLevel.Error,
        Message = "Container name cannot be null or empty")]
    public static partial IGenericMessage ContainerNameRequired(ILogger logger);

    /// <summary>
    /// Logs when store type is not supported.
    /// </summary>
    [MessageLogging(
        EventId = 61004,
        Level = LogLevel.Error,
        Message = "Store type '{storeType}' is not supported by MsSql discoverer")]
    public static partial IGenericMessage StoreTypeNotSupported(ILogger logger, string storeType);

    /// <summary>
    /// Logs when DataStore location is not set.
    /// </summary>
    [MessageLogging(
        EventId = 21009,
        Level = LogLevel.Error,
        Message = "DataStore location (connection name) is not set")]
    public static partial IGenericMessage DataStoreLocationNotSet(ILogger logger);

    /// <summary>
    /// Logs when failed to get connection.
    /// </summary>
    [MessageLogging(
        EventId = 71030,
        Level = LogLevel.Error,
        Message = "Failed to get connection")]
    public static partial IGenericMessage FailedToGetConnection(ILogger logger);

    /// <summary>
    /// Logs when foreign keys are discovered for a container.
    /// </summary>
    [MessageLogging(
        EventId = 11051,
        Level = LogLevel.Debug,
        Message = "Discovered {foreignKeyCount} foreign keys for '{schemaName}.{objectName}'")]
    public static partial IGenericMessage ForeignKeysDiscovered(ILogger logger, int foreignKeyCount, string schemaName, string objectName);

    /// <summary>
    /// Logs when foreign key discovery fails.
    /// </summary>
    [MessageLogging(
        EventId = 71031,
        Level = LogLevel.Warning,
        Message = "Foreign key discovery failed for '{schemaName}.{objectName}': {errorMessage}")]
    public static partial IGenericMessage ForeignKeyDiscoveryFailed(ILogger logger, string schemaName, string objectName, string errorMessage);

    /// <summary>
    /// Logs when description discovery fails.
    /// </summary>
    [MessageLogging(
        EventId = 71032,
        Level = LogLevel.Warning,
        Message = "Description discovery failed for '{schemaName}.{objectName}': {errorMessage}")]
    public static partial IGenericMessage DescriptionDiscoveryFailed(ILogger logger, string schemaName, string objectName, string errorMessage);

    /// <summary>
    /// Logs when schema auto-discovery starts.
    /// </summary>
    [MessageLogging(
        EventId = 11052,
        Level = LogLevel.Information,
        Message = "Starting schema auto-discovery for connection '{connectionName}'")]
    public static partial IGenericMessage SchemaAutoDiscoveryStarted(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when schema auto-discovery completes successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11053,
        Level = LogLevel.Information,
        Message = "Schema auto-discovery completed for connection '{connectionName}': DataStore '{dataStoreId}', {pathsDiscovered} paths, {containersDiscovered} containers, {fieldsDiscovered} fields")]
    public static partial IGenericMessage SchemaAutoDiscoveryCompleted(ILogger logger, string connectionName, Guid dataStoreId, int pathsDiscovered, int containersDiscovered, int fieldsDiscovered);

    /// <summary>
    /// Logs when schema auto-discovery fails.
    /// </summary>
    [MessageLogging(
        EventId = 71033,
        Level = LogLevel.Error,
        Message = "Schema auto-discovery failed for connection '{connectionName}': {errorMessage}")]
    public static partial IGenericMessage SchemaAutoDiscoveryFailed(ILogger logger, string connectionName, string errorMessage);

    /// <summary>
    /// Logs when schema auto-discovery is skipped because service is not available.
    /// </summary>
    [MessageLogging(
        EventId = 11054,
        Level = LogLevel.Debug,
        Message = "Schema auto-discovery skipped for connection '{connectionName}' - service not available")]
    public static partial IGenericMessage SchemaAutoDiscoverySkipped(ILogger logger, string connectionName);

    // ═══════════════════════════════════════════════════════════════════════════
    // MsSqlConnectionType Registration Events (5210-5229)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when RegisterFactory starts.
    /// </summary>
    [MessageLogging(
        EventId = 11055,
        Level = LogLevel.Information,
        Message = "[MsSql] RegisterFactory starting - resolving factory from DI")]
    public static partial IGenericMessage RegisterFactoryStarting(ILogger logger);

    /// <summary>
    /// Logs when factory is resolved from DI.
    /// </summary>
    [MessageLogging(
        EventId = 11056,
        Level = LogLevel.Information,
        Message = "[MsSql] Factory resolved - registering with provider")]
    public static partial IGenericMessage FactoryResolved(ILogger logger);

    /// <summary>
    /// Logs when resolving configuration options.
    /// </summary>
    [MessageLogging(
        EventId = 11057,
        Level = LogLevel.Information,
        Message = "[MsSql] Resolving IOptionsMonitor<List<MsSqlConnectionConfiguration>>")]
    public static partial IGenericMessage ResolvingConfigurationOptions(ILogger logger);

    /// <summary>
    /// Logs when configuration provider is registered.
    /// </summary>
    [MessageLogging(
        EventId = 11058,
        Level = LogLevel.Information,
        Message = "[MsSql] Configuration provider registered - checking for schema discovery")]
    public static partial IGenericMessage ConfigurationProviderRegistered(ILogger logger);

    /// <summary>
    /// Logs when skipping schema discovery due to missing writer factory.
    /// </summary>
    [MessageLogging(
        EventId = 11059,
        Level = LogLevel.Information,
        Message = "[MsSql] No IConfigurationWriterFactory available - skipping schema discovery")]
    public static partial IGenericMessage SkippingSchemaDiscoveryNoWriter(ILogger logger);

    /// <summary>
    /// Logs when resolving DataStore options.
    /// </summary>
    [MessageLogging(
        EventId = 11060,
        Level = LogLevel.Information,
        Message = "[MsSql] Resolving DataStore/DataPath/DataContainer options")]
    public static partial IGenericMessage ResolvingDataStoreOptions(ILogger logger);

    /// <summary>
    /// Logs when no connections are configured.
    /// </summary>
    [MessageLogging(
        EventId = 11061,
        Level = LogLevel.Information,
        Message = "[MsSql] No MsSql connections configured - skipping schema discovery")]
    public static partial IGenericMessage NoConnectionsConfigured(ILogger logger);

    /// <summary>
    /// Logs when starting schema discovery for connections.
    /// </summary>
    [MessageLogging(
        EventId = 11062,
        Level = LogLevel.Information,
        Message = "[MsSql] Starting schema discovery for {count} connections")]
    public static partial IGenericMessage StartingSchemaDiscoveryForConnections(ILogger logger, int count);

    /// <summary>
    /// Logs when processing a connection for schema discovery.
    /// </summary>
    [MessageLogging(
        EventId = 11063,
        Level = LogLevel.Information,
        Message = "[MsSql] Processing connection '{connectionName}' - creating connection")]
    public static partial IGenericMessage ProcessingConnection(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when connection is created and discovering schema.
    /// </summary>
    [MessageLogging(
        EventId = 11064,
        Level = LogLevel.Information,
        Message = "[MsSql] Connection '{connectionName}' created - discovering schema")]
    public static partial IGenericMessage ConnectionCreatedDiscoveringSchema(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when schema is discovered and persisting.
    /// </summary>
    [MessageLogging(
        EventId = 11065,
        Level = LogLevel.Information,
        Message = "[MsSql] Schema discovered for '{connectionName}' - persisting")]
    public static partial IGenericMessage SchemaDiscoveredPersisting(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when schema persistence completes.
    /// </summary>
    [MessageLogging(
        EventId = 11066,
        Level = LogLevel.Information,
        Message = "[MsSql] Persisted schema for connection '{connectionName}': {pathCount} paths, {containerCount} containers")]
    public static partial IGenericMessage SchemaPersisted(ILogger logger, string connectionName, int pathCount, int containerCount);

    /// <summary>
    /// Logs when schema persistence fails.
    /// </summary>
    [MessageLogging(
        EventId = 71034,
        Level = LogLevel.Error,
        Message = "[MsSql] Failed to persist schema for connection '{connectionName}': {error}")]
    public static partial IGenericMessage SchemaPersistedFailed(ILogger logger, string connectionName, string error);

    /// <summary>
    /// Logs when connection creation fails for schema discovery.
    /// </summary>
    [MessageLogging(
        EventId = 71035,
        Level = LogLevel.Error,
        Message = "[MsSql] Failed to create connection '{connectionName}' for schema discovery: {error}")]
    public static partial IGenericMessage ConnectionCreationFailed(ILogger logger, string connectionName, string error);

    /// <summary>
    /// Logs when schema discovery fails for a connection.
    /// </summary>
    [MessageLogging(
        EventId = 71036,
        Level = LogLevel.Error,
        Message = "[MsSql] Failed to discover schema for connection '{connectionName}': {error}")]
    public static partial IGenericMessage SchemaDiscoveryFailedForConnection(ILogger logger, string connectionName, string error);

    /// <summary>
    /// Logs when schema discovery throws an exception.
    /// </summary>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Error,
        Message = "[MsSql] Schema discovery exception for connection '{connectionName}'")]
    public static partial IGenericMessage SchemaDiscoveryException(ILogger logger, Exception exception, string connectionName);

    /// <summary>
    /// Logs when schema discoverer is not registered in MsSqlConnectionType.
    /// </summary>
    [MessageLogging(EventId = 61005, Level = LogLevel.Error,
        Message = "[MsSql] Schema discoverer not registered - ensure IMsSqlSchemaDiscoverer is registered in DI")]
    public static partial IGenericMessage SchemaDiscovererNotRegistered(ILogger logger);

    /// <summary>
    /// Logs when connection type mismatch occurs during DiscoverSchema.
    /// </summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "[MsSql] DiscoverSchema called with wrong connection type — expected MsSqlConnection")]
    public static partial IGenericMessage ConnectionTypeMismatch(ILogger logger);

    // ── Keys of a discovery (FDW-6309) ─────────────────────────────────────────

    /// <summary>
    /// Logs a foreign key whose referenced container the discovery did not read.
    /// </summary>
    [MessageLogging(
        EventId = 41010,
        Level = LogLevel.Warning,
        Message = "Foreign key '{keyName}' of '{schemaName}.{tableName}' references '{referencedSchema}.{referencedTable}', which this discovery did not read (its schema is left out, or it is not a table). The key stays on its table, its columns are foreign key columns, and no relationship is drawn")]
    public static partial IGenericMessage ForeignKeyTargetNotDiscovered(ILogger logger, string keyName, string schemaName, string tableName, string referencedSchema, string referencedTable);

    /// <summary>
    /// Logs a table whose key metadata the login cannot see.
    /// </summary>
    [MessageLogging(
        EventId = 41011,
        Level = LogLevel.Warning,
        Message = "The login cannot see the key metadata of '{schemaName}.{tableName}' (VIEW DEFINITION on it is missing), so its primary key and foreign keys are not known")]
    public static partial IGenericMessage KeyMetadataHidden(ILogger logger, string schemaName, string tableName);

    /// <summary>
    /// Logs a table that has a primary key the discovery read none of.
    /// </summary>
    [MessageLogging(
        EventId = 41012,
        Level = LogLevel.Warning,
        Message = "Table '{schemaName}.{tableName}' has a primary key, and the catalog read through this login returned none of its columns (VIEW DEFINITION on it may be missing), so its primary key is not known")]
    public static partial IGenericMessage PrimaryKeyNotRead(ILogger logger, string schemaName, string tableName);

    /// <summary>
    /// Logs a table that has foreign keys the discovery read none of.
    /// </summary>
    [MessageLogging(
        EventId = 41013,
        Level = LogLevel.Warning,
        Message = "Table '{schemaName}.{tableName}' has foreign keys, and the catalog read through this login returned none of them (VIEW DEFINITION on it may be missing), so its foreign keys are not known")]
    public static partial IGenericMessage ForeignKeysNotRead(ILogger logger, string schemaName, string tableName);

    /// <summary>
    /// Logs a key that names a column its table does not have, which refuses the discovery.
    /// </summary>
    [MessageLogging(
        EventId = 91004,
        Level = LogLevel.Error,
        Message = "Key '{keyName}' of '{schemaName}.{tableName}' names column '{columnName}', which the discovery did not read for that table")]
    public static partial IGenericMessage KeyColumnNotFound(ILogger logger, string keyName, string schemaName, string tableName, string columnName);

    // ── Container types of a discovery (FDW-6309, FDW-6387) ────────────────────

    /// <summary>
    /// Logs a container type word the SQL Server discoverer wrote that this connection type maps to no container type option.
    /// </summary>
    [MessageLogging(
        EventId = 31010,
        Level = LogLevel.Error,
        Message = "The discovery named a container of type '{containerTypeWord}', which SQL Server maps to no container type option (it maps Table, View and StoredProcedure)")]
    public static partial IGenericMessage ContainerTypeWordNotMapped(ILogger logger, string containerTypeWord);

    /// <summary>
    /// Logs a container type option SQL Server discovery resolves that ContainerTypes does not hold.
    /// </summary>
    [MessageLogging(
        EventId = 31011,
        Level = LogLevel.Error,
        Message = "Container type option '{optionName}' is not registered in ContainerTypes, so a discovered SQL Server container cannot be given its type")]
    public static partial IGenericMessage ContainerTypeNotRegistered(ILogger logger, string optionName);
}
