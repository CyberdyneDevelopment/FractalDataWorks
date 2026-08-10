using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Schema.Endpoints;

/// <summary>
/// MessageLogging for Web.Api CRUD endpoint operations.
/// EventId range: 4500-4530
/// </summary>
[MessageLoggingTypeCode("SCHEMAENDPOINTS")]
public static partial class SchemaEndpointLog
{

    // ═══════════════════════════════════════════════════════════════════════════
    // Schema Discovery Operations (4517-4530)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs that schema discovery is starting for a connection.</summary>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Information,
        Message = "Discovering schema for connection '{connectionName}'")]
    public static partial IGenericMessage DiscoveringSchema(
        ILogger logger,
        string connectionName);

    /// <summary>Logs that schema discovery completed successfully.</summary>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Information,
        Message = "Schema discovered for connection '{connectionName}': {schemaCount} schemas found")]
    public static partial IGenericMessage SchemaDiscovered(
        ILogger logger,
        string connectionName,
        int schemaCount);

    /// <summary>Logs that a data preview operation is starting.</summary>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Debug,
        Message = "Previewing data from '{source}' (max {maxRows} rows)")]
    public static partial IGenericMessage PreviewingData(
        ILogger logger,
        string source,
        int maxRows);

    /// <summary>Logs that a data preview completed successfully.</summary>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Information,
        Message = "Data previewed from '{source}': {rowCount} rows returned")]
    public static partial IGenericMessage DataPreviewed(
        ILogger logger,
        string source,
        int rowCount);

    /// <summary>Logs that schema import is starting for a connection.</summary>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Information,
        Message = "Importing schema from connection '{connectionName}' to DataStore '{dataStoreName}'")]
    public static partial IGenericMessage ImportingSchema(
        ILogger logger,
        string connectionName,
        string dataStoreName);

    /// <summary>Logs that schema import completed successfully.</summary>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Information,
        Message = "Schema imported from '{connectionName}': {tableCount} tables, {columnCount} columns")]
    public static partial IGenericMessage SchemaImported(
        ILogger logger,
        string connectionName,
        int tableCount,
        int columnCount);

    /// <summary>Logs that schema sync is starting for a connection.</summary>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Information,
        Message = "Syncing schema for connection '{connectionName}'")]
    public static partial IGenericMessage SyncingSchema(
        ILogger logger,
        string connectionName);

    /// <summary>Logs that schema sync completed successfully.</summary>
    [MessageLogging(
        EventId = 11018,
        Level = LogLevel.Information,
        Message = "Schema synced for '{connectionName}': {addedCount} added, {modifiedCount} modified, {removedCount} removed")]
    public static partial IGenericMessage SchemaSynced(
        ILogger logger,
        string connectionName,
        int addedCount,
        int modifiedCount,
        int removedCount);

    /// <summary>Logs an error when a schema operation fails with an exception.</summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Schema {operation} failed for '{connectionName}'")]
    public static partial IGenericMessage SchemaOperationFailed(
        ILogger logger,
        Exception exception,
        string operation,
        string connectionName);

    /// <summary>Logs that listing of schema-capable connections is starting.</summary>
    [MessageLogging(
        EventId = 11019,
        Level = LogLevel.Debug,
        Message = "Listing schema-capable connections")]
    public static partial IGenericMessage ListingSchemaConnections(
        ILogger logger);

    /// <summary>Logs the count of schema-capable connections found.</summary>
    [MessageLogging(
        EventId = 11020,
        Level = LogLevel.Information,
        Message = "Found {count} schema-capable connections")]
    public static partial IGenericMessage SchemaConnectionsListed(
        ILogger logger,
        int count);

    /// <summary>Logs a warning when a connection is not found during a schema operation.</summary>
    [MessageLogging(
        EventId = 31001,
        Level = LogLevel.Warning,
        Message = "Connection '{connectionName}' not found for schema {operation}")]
    public static partial IGenericMessage SchemaConnectionNotFound(
        ILogger logger,
        string connectionName,
        string operation);

    /// <summary>Logs a warning when a connection type does not support schema discovery.</summary>
    [MessageLogging(
        EventId = 61001,
        Level = LogLevel.Warning,
        Message = "Connection '{connectionName}' does not support schema discovery")]
    public static partial IGenericMessage SchemaDiscoveryNotSupported(
        ILogger logger,
        string connectionName);

    /// <summary>Logs a warning when data preview validation fails.</summary>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Warning,
        Message = "Data preview validation failed: {reason}")]
    public static partial IGenericMessage PreviewValidationFailed(
        ILogger logger,
        string reason);

    /// <summary>Logs a warning when a container-address field is missing in a data preview request.</summary>
    [MessageLogging(
        EventId = 21002,
        Level = LogLevel.Warning,
        Message = "Data preview container-address is incomplete: {missingField} is required")]
    public static partial IGenericMessage PreviewContainerAddressMissing(
        ILogger logger,
        string missingField);
}
