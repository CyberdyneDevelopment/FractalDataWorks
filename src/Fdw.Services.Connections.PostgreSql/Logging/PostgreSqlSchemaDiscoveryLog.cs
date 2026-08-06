using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.PostgreSql.Logging;

/// <summary>
/// MessageLogging for PostgreSQL schema discovery operations.
/// EventId range: 8800-8849 (Schema Discovery - PostgreSQL specific)
/// </summary>
[MessageLoggingTypeCode("PGSQL")]
public static partial class PostgreSqlSchemaDiscoveryLog
{
    /// <summary>
    /// Logs when schema discovery starts for a database.
    /// </summary>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Information,
        Message = "Starting PostgreSQL schema discovery for database '{databaseName}'")]
    public static partial IGenericMessage SchemaDiscoveryStarted(ILogger logger, string databaseName);

    /// <summary>
    /// Logs when schemas have been discovered.
    /// </summary>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Information,
        Message = "Discovered {schemaCount} PostgreSQL schemas")]
    public static partial IGenericMessage SchemasDiscovered(ILogger logger, int schemaCount);

    /// <summary>
    /// Logs when a container (table/view) is discovered.
    /// </summary>
    [MessageLogging(
        EventId = 11018,
        Level = LogLevel.Debug,
        Message = "Discovered PostgreSQL {containerType} '{schemaName}.{objectName}' with {fieldCount} fields")]
    public static partial IGenericMessage ContainerDiscovered(ILogger logger, string containerType, string schemaName, string objectName, int fieldCount);

    /// <summary>
    /// Logs when schema discovery completes successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11019,
        Level = LogLevel.Information,
        Message = "PostgreSQL schema discovery completed: {pathCount} paths, {containerCount} containers, {fieldCount} fields")]
    public static partial IGenericMessage SchemaDiscoveryCompleted(ILogger logger, int pathCount, int containerCount, int fieldCount);

    /// <summary>
    /// Logs when schema discovery fails.
    /// </summary>
    [MessageLogging(
        EventId = 71004,
        Level = LogLevel.Error,
        Message = "PostgreSQL schema discovery failed: {errorMessage}")]
    public static partial IGenericMessage SchemaDiscoveryFailed(ILogger logger, string errorMessage);

    /// <summary>
    /// Logs when connection fails during discovery.
    /// </summary>
    [MessageLogging(
        EventId = 71005,
        Level = LogLevel.Error,
        Message = "PostgreSQL connection failed during discovery: {errorMessage}")]
    public static partial IGenericMessage ConnectionFailed(ILogger logger, string errorMessage);

    /// <summary>
    /// Logs when a container is not found.
    /// </summary>
    [MessageLogging(
        EventId = 31002,
        Level = LogLevel.Warning,
        Message = "PostgreSQL container '{schemaName}.{objectName}' not found in database")]
    public static partial IGenericMessage ContainerNotFound(ILogger logger, string schemaName, string objectName);

    /// <summary>
    /// Logs when container discovery starts.
    /// </summary>
    [MessageLogging(
        EventId = 11020,
        Level = LogLevel.Debug,
        Message = "Starting PostgreSQL container discovery for '{schemaName}.{objectName}'")]
    public static partial IGenericMessage ContainerDiscoveryStarted(ILogger logger, string schemaName, string objectName);

    /// <summary>
    /// Logs when container discovery fails.
    /// </summary>
    [MessageLogging(
        EventId = 71006,
        Level = LogLevel.Error,
        Message = "PostgreSQL container discovery failed for '{schemaName}.{objectName}': {errorMessage}")]
    public static partial IGenericMessage ContainerDiscoveryFailed(ILogger logger, string schemaName, string objectName, string errorMessage);

    /// <summary>
    /// Logs when containers are registered from discovery.
    /// </summary>
    [MessageLogging(
        EventId = 11021,
        Level = LogLevel.Information,
        Message = "Registered {containerCount} containers from PostgreSQL discovery for '{dataStoreName}'")]
    public static partial IGenericMessage ContainersRegisteredFromDiscovery(ILogger logger, int containerCount, string dataStoreName);

    /// <summary>
    /// Logs when DataStore is null.
    /// </summary>
    [MessageLogging(
        EventId = 21004,
        Level = LogLevel.Error,
        Message = "DataStore cannot be null for PostgreSQL discovery")]
    public static partial IGenericMessage DataStoreNull(ILogger logger);

    /// <summary>
    /// Logs when schema name is null or empty.
    /// </summary>
    [MessageLogging(
        EventId = 21005,
        Level = LogLevel.Error,
        Message = "Schema name cannot be null or empty for PostgreSQL discovery")]
    public static partial IGenericMessage SchemaNameRequired(ILogger logger);

    /// <summary>
    /// Logs when container name is null or empty.
    /// </summary>
    [MessageLogging(
        EventId = 21006,
        Level = LogLevel.Error,
        Message = "Container name cannot be null or empty for PostgreSQL discovery")]
    public static partial IGenericMessage ContainerNameRequired(ILogger logger);

    /// <summary>
    /// Logs when store type is not supported.
    /// </summary>
    [MessageLogging(
        EventId = 61002,
        Level = LogLevel.Error,
        Message = "Store type '{storeType}' is not supported by PostgreSQL discoverer")]
    public static partial IGenericMessage StoreTypeNotSupported(ILogger logger, string storeType);

    /// <summary>
    /// Logs when DataStore location is not set.
    /// </summary>
    [MessageLogging(
        EventId = 21007,
        Level = LogLevel.Error,
        Message = "DataStore location (connection name) is not set for PostgreSQL discovery")]
    public static partial IGenericMessage DataStoreLocationNotSet(ILogger logger);

    /// <summary>
    /// Logs when the schema discoverer is not registered.
    /// </summary>
    [MessageLogging(
        EventId = 61003,
        Level = LogLevel.Error,
        Message = "[PostgreSql] Schema discoverer not registered - ensure IPostgreSqlSchemaDiscoverer is registered in DI")]
    public static partial IGenericMessage SchemaDiscovererNotRegistered(ILogger logger);

    /// <summary>
    /// Logs when connection type mismatch occurs during DiscoverSchema.
    /// </summary>
    [MessageLogging(
        EventId = 21008,
        Level = LogLevel.Error,
        Message = "[PostgreSql] DiscoverSchema called with wrong connection type - expected PostgreSqlConnection")]
    public static partial IGenericMessage ConnectionTypeMismatch(ILogger logger);

    /// <summary>
    /// Logs when discovery test fails.
    /// </summary>
    [MessageLogging(
        EventId = 71007,
        Level = LogLevel.Error,
        Message = "PostgreSQL discovery test failed: {errorMessage}")]
    public static partial IGenericMessage DiscoveryTestFailed(ILogger logger, string errorMessage);

    /// <summary>
    /// Logs when foreign keys are discovered for a container.
    /// </summary>
    [MessageLogging(
        EventId = 11022,
        Level = LogLevel.Debug,
        Message = "Discovered {foreignKeyCount} foreign keys for PostgreSQL '{schemaName}.{objectName}'")]
    public static partial IGenericMessage ForeignKeysDiscovered(ILogger logger, int foreignKeyCount, string schemaName, string objectName);

    /// <summary>
    /// Logs when schema auto-discovery starts.
    /// </summary>
    [MessageLogging(
        EventId = 11023,
        Level = LogLevel.Information,
        Message = "Starting PostgreSQL schema auto-discovery for connection '{connectionName}'")]
    public static partial IGenericMessage SchemaAutoDiscoveryStarted(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when schema auto-discovery completes successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11024,
        Level = LogLevel.Information,
        Message = "PostgreSQL schema auto-discovery completed for connection '{connectionName}': {pathsDiscovered} paths, {containersDiscovered} containers, {fieldsDiscovered} fields")]
    public static partial IGenericMessage SchemaAutoDiscoveryCompleted(ILogger logger, string connectionName, int pathsDiscovered, int containersDiscovered, int fieldsDiscovered);

    /// <summary>
    /// Logs when schema auto-discovery fails.
    /// </summary>
    [MessageLogging(
        EventId = 71008,
        Level = LogLevel.Error,
        Message = "PostgreSQL schema auto-discovery failed for connection '{connectionName}': {errorMessage}")]
    public static partial IGenericMessage SchemaAutoDiscoveryFailed(ILogger logger, string connectionName, string errorMessage);

    /// <summary>
    /// Logs a schema discovery exception.
    /// </summary>
    [MessageLogging(
        EventId = 71009,
        Level = LogLevel.Error,
        Message = "[PostgreSql] Schema discovery exception for connection '{connectionName}'")]
    public static partial IGenericMessage SchemaDiscoveryException(ILogger logger, Exception exception, string connectionName);
}
