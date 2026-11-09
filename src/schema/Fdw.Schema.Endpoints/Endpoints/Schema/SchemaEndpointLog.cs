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
        Level = LogLevel.Trace,
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
        Level = LogLevel.Trace,
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
        Level = LogLevel.Trace,
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

    /// <summary>Logs that a data preview was answered with its failure instead of rows.</summary>
    // Why Information: the gateway already logged why the read failed, at that failure's own level. This line only
    // records that the endpoint answered it once, with that status, and reported no rows.
    [MessageLogging(
        EventId = 11021,
        Level = LogLevel.Information,
        Message = "Data preview of '{source}' was answered with its failure; no rows were returned")]
    public static partial IGenericMessage PreviewNotAnswered(
        ILogger logger,
        string source);

    /// <summary>Logs that the request failed: Schema graph retrieval failed.</summary>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error, Message = "Schema graph retrieval failed", ClientMessage = "Schema graph retrieval failed")]
    public static partial IGenericMessage GetSchemaGraphFaulted(ILogger logger);

    /// <summary>Logs that the request failed: Schema discovery failed.</summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error, Message = "Schema discovery failed", ClientMessage = "Schema discovery failed")]
    public static partial IGenericMessage DiscoverSchemaFaulted(ILogger logger);

    /// <summary>Logs that the request failed: TableName is required.</summary>
    [MessageLogging(EventId = 21003, Level = LogLevel.Warning, Message = "TableName is required", ClientMessage = "TableName is required")]
    public static partial IGenericMessage ExecuteDdlTablenameIsRequired(ILogger logger);

    /// <summary>Logs that the request failed: At least one column is required.</summary>
    [MessageLogging(EventId = 21004, Level = LogLevel.Warning, Message = "At least one column is required", ClientMessage = "At least one column is required")]
    public static partial IGenericMessage ExecuteDdlAtLeastOneColumnIs(ILogger logger);

    /// <summary>Logs that the request failed: Unable to determine connection type.</summary>
    [MessageLogging(EventId = 21005, Level = LogLevel.Warning, Message = "Unable to determine connection type", ClientMessage = "Unable to determine connection type")]
    public static partial IGenericMessage GenerateDdlUnableToDetermineConnectionType(ILogger logger);

    /// <summary>Logs that the request failed: DDL generation failed.</summary>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error, Message = "DDL generation failed", ClientMessage = "DDL generation failed")]
    public static partial IGenericMessage GenerateDdlFaulted(ILogger logger);

    /// <summary>Logs that the request failed: Schema sync failed.</summary>
    [MessageLogging(EventId = 91005, Level = LogLevel.Error, Message = "Schema sync failed", ClientMessage = "Schema sync failed")]
    public static partial IGenericMessage SyncSchemaFaulted(ILogger logger);

    /// <summary>Logs that the request failed: Failed to list schema-capable connections.</summary>
    [MessageLogging(EventId = 91006, Level = LogLevel.Error, Message = "Failed to list schema-capable connections", ClientMessage = "Failed to list schema-capable connections")]
    public static partial IGenericMessage ListSchemaCapableConnectionsFailedToListSchemaCapable(ILogger logger);

    /// <summary>Logs that the request failed: Either DataSetName or ContainerName must be provided.</summary>
    [MessageLogging(EventId = 21006, Level = LogLevel.Warning, Message = "Either DataSetName or ContainerName must be provided", ClientMessage = "Either DataSetName or ContainerName must be provided")]
    public static partial IGenericMessage DataPreviewEitherDatasetnameOrContainernameMust(ILogger logger);

    /// <summary>Logs that the request failed: DataStoreName is required when ContainerName is provided.</summary>
    [MessageLogging(EventId = 21007, Level = LogLevel.Warning, Message = "DataStoreName is required when ContainerName is provided", ClientMessage = "DataStoreName is required when ContainerName is provided")]
    public static partial IGenericMessage DataPreviewDatastorenameIsRequiredWhenContainername(ILogger logger);

    /// <summary>Logs that the request failed: PathName is required when ContainerName is provided.</summary>
    [MessageLogging(EventId = 21008, Level = LogLevel.Warning, Message = "PathName is required when ContainerName is provided", ClientMessage = "PathName is required when ContainerName is provided")]
    public static partial IGenericMessage DataPreviewPathnameIsRequiredWhenContainername(ILogger logger);

    /// <summary>Logs that Connection 'connectionName' was not found for discoverschema.</summary>
    [MessageLogging(EventId = 31002, Level = LogLevel.Warning, Message = "Connection '{connectionName}' was not found for discoverschema", ClientMessage = "Connection '{connectionName}' was not found")]
    public static partial IGenericMessage DiscoverSchemaConnectionNotFound(ILogger logger, string connectionName);

    /// <summary>Logs that Connection 'connectionName' was not found for getschemagraph.</summary>
    [MessageLogging(EventId = 31003, Level = LogLevel.Warning, Message = "Connection '{connectionName}' was not found for getschemagraph", ClientMessage = "Connection '{connectionName}' was not found")]
    public static partial IGenericMessage GetSchemaGraphConnectionNotFound(ILogger logger, string connectionName);

    /// <summary>Logs that Connection 'connectionName' was not found for generateddl.</summary>
    [MessageLogging(EventId = 31004, Level = LogLevel.Warning, Message = "Connection '{connectionName}' was not found for generateddl", ClientMessage = "Connection '{connectionName}' was not found")]
    public static partial IGenericMessage GenerateDdlConnectionNotFound(ILogger logger, string connectionName);

    /// <summary>Logs that Connection 'connectionName' was not found for syncschema.</summary>
    [MessageLogging(EventId = 31005, Level = LogLevel.Warning, Message = "Connection '{connectionName}' was not found for syncschema", ClientMessage = "Connection '{connectionName}' was not found")]
    public static partial IGenericMessage SyncSchemaConnectionNotFound(ILogger logger, string connectionName);

    /// <summary>Logs that Importing the schema of connection 'connectionName' threw.</summary>
    [MessageLogging(EventId = 91007, Level = LogLevel.Error, Message = "Importing the schema of connection '{connectionName}' threw", ClientMessage = "An internal error occurred during schema import")]
    public static partial IGenericMessage ImportSchemaFaulted(ILogger logger, string connectionName);

    /// <summary>Logs that Importing the schema of connection 'connectionName' answered a success and no value.</summary>
    [MessageLogging(EventId = 91008, Level = LogLevel.Error, Message = "Importing the schema of connection '{connectionName}' answered a success and no value", ClientMessage = "The schema could not be imported")]
    public static partial IGenericMessage ImportSchemaReturnedNothing(ILogger logger, string connectionName);

    /// <summary>Logs that execute ddl failed unexpectedly.</summary>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error, Message = "Execute ddl failed unexpectedly", ClientMessage = "Execute ddl failed unexpectedly")]
    public static partial IGenericMessage ExecuteDdlFaulted(ILogger logger);
}
