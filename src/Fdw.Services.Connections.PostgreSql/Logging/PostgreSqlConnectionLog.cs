using System;
using Fdw.Configuration;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.PostgreSql.Logging;

/// <summary>
/// MessageLogging for PostgreSQL connection operations.
/// EventId range: 5260-5290
/// </summary>
[MessageLoggingTypeCode("PGSQL")]
public static partial class PostgreSqlConnectionLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Trace (5261-5264)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when building a PostgreSQL connection string.
    /// </summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace, Message = "Building PostgreSQL connection string for '{connectionName}'")]
    public static partial IGenericMessage BuildingConnectionString(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when resolving a secret manager for password retrieval.
    /// </summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace, Message = "Resolving secret manager for PostgreSQL connection '{connectionName}'")]
    public static partial IGenericMessage ResolvingSecretManager(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when an authentication processor is selected.
    /// </summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace, Message = "Authentication processor '{processorName}' selected for PostgreSQL connection '{connectionName}'")]
    public static partial IGenericMessage AuthProcessorSelected(ILogger logger, string processorName, string connectionName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Debug (5265-5268)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs resolved PostgreSQL configuration details.
    /// </summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug, Message = "PostgreSQL configuration resolved for '{connectionName}': Host={host}, Database={database}, Port={port}")]
    public static partial IGenericMessage ConfigurationResolved(ILogger logger, string connectionName, string host, string database, int port);

    /// <summary>
    /// Logs connection pool settings.
    /// </summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Debug, Message = "PostgreSQL connection pool settings for '{connectionName}': MinPoolSize={minPoolSize}, MaxPoolSize={maxPoolSize}")]
    public static partial IGenericMessage ConnectionPoolSettings(ILogger logger, string connectionName, int minPoolSize, int maxPoolSize);

    /// <summary>
    /// Logs the SSL mode applied to a connection.
    /// </summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Debug, Message = "SSL mode '{sslMode}' applied for PostgreSQL connection '{connectionName}'")]
    public static partial IGenericMessage SslModeApplied(ILogger logger, string sslMode, string connectionName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Information (5270-5274)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a PostgreSQL connection is successfully created.
    /// </summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information, Message = "PostgreSQL connection created for '{connectionName}' to {host}:{port}/{database}")]
    public static partial IGenericMessage ConnectionCreated(ILogger logger, string connectionName, string host, int port, string database);

    /// <summary>
    /// Logs when a PostgreSQL connection is opened.
    /// </summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Information, Message = "PostgreSQL connection opened for '{connectionName}'")]
    public static partial IGenericMessage ConnectionOpened(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when a PostgreSQL connection is closed.
    /// </summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Information, Message = "PostgreSQL connection closed for '{connectionName}'")]
    public static partial IGenericMessage ConnectionClosed(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when schema discovery completes for a connection.
    /// </summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Information, Message = "PostgreSQL schema discovered for '{connectionName}': {schemaCount} schemas")]
    public static partial IGenericMessage SchemaDiscovered(ILogger logger, string connectionName, int schemaCount);

    // ═══════════════════════════════════════════════════════════════════════════
    // Warning (5275-5277)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when the connection pool is exhausted.
    /// </summary>
    [MessageLogging(EventId = 81000, Level = LogLevel.Warning, Message = "PostgreSQL connection pool exhausted for '{connectionName}'")]
    public static partial IGenericMessage ConnectionPoolExhausted(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when a slow query is detected.
    /// </summary>
    [MessageLogging(EventId = 81001, Level = LogLevel.Warning, Message = "Slow query detected on PostgreSQL connection '{connectionName}': {elapsedMs}ms")]
    public static partial IGenericMessage SlowQuery(ILogger logger, string connectionName, long elapsedMs);

    /// <summary>
    /// Logs that data command execution is not supported.
    /// </summary>
    [MessageLogging(EventId = 61000, Level = LogLevel.Error, Message = "Data command execution is not supported on PostgreSQL connections in this reference implementation")]
    public static partial IGenericMessage ExecutionNotSupported(ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error (5280-5290)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a PostgreSQL connection attempt fails.
    /// </summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "PostgreSQL connection failed for '{connectionName}'")]
    public static partial IGenericMessage ConnectionFailed(ILogger logger, Exception ex, string connectionName);

    /// <summary>
    /// Logs when PostgreSQL authentication fails.
    /// </summary>
    [MessageLogging(EventId = 51000, Level = LogLevel.Error, Message = "PostgreSQL authentication failed for '{connectionName}': {errorMessage}")]
    public static partial IGenericMessage AuthenticationFailed(ILogger logger, string connectionName, string errorMessage);

    /// <summary>
    /// Logs when a PostgreSQL query fails.
    /// </summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error, Message = "PostgreSQL query failed on '{connectionName}'")]
    public static partial IGenericMessage QueryFailed(ILogger logger, Exception ex, string connectionName);

    /// <summary>
    /// Logs when the factory receives a null configuration.
    /// </summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error, Message = "PostgreSQL factory received null configuration")]
    public static partial IGenericMessage ConfigurationNull(ILogger logger);

    /// <summary>
    /// Logs when an invalid configuration type is passed to the factory.
    /// </summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Error, Message = "Invalid configuration type for PostgreSQL. Expected PostgreSqlConnectionConfiguration, got '{actualType}'")]
    public static partial IGenericMessage InvalidConfigurationType(ILogger logger, string actualType);

    /// <summary>
    /// Logs when PostgreSQL connection creation fails.
    /// </summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error, Message = "Failed to create PostgreSQL connection for '{connectionName}'")]
    public static partial IGenericMessage CreationFailed(ILogger logger, Exception ex, string connectionName);

    /// <summary>
    /// Logs when an unsupported connection type is requested.
    /// </summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Error, Message = "Unsupported connection type: '{connectionType}'. This factory only supports PostgreSql.")]
    public static partial IGenericMessage UnsupportedConnectionType(ILogger logger, string connectionType);

    /// <summary>
    /// Logs when the secret manager provider is not available.
    /// </summary>
    [MessageLogging(EventId = 61001, Level = LogLevel.Error, Message = "Secret manager provider not available for PostgreSQL connection '{connectionName}', secret '{secretKeyName}'")]
    public static partial IGenericMessage SecretManagerProviderNotAvailable(ILogger logger, string connectionName, string secretKeyName);

    /// <summary>
    /// Logs when a secret manager is not found by name.
    /// </summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Error, Message = "Secret manager '{secretManagerName}' not found for PostgreSQL connection '{connectionName}'")]
    public static partial IGenericMessage SecretManagerNotFound(ILogger logger, string connectionName, string secretManagerName);

    /// <summary>
    /// Logs when a secret key is not found in the secret manager.
    /// </summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error, Message = "Secret '{secretKeyName}' not found for PostgreSQL connection '{connectionName}'")]
    public static partial IGenericMessage SecretNotFound(ILogger logger, string connectionName, string secretKeyName);

    /// <summary>
    /// Logs when authentication type is not specified in the configuration.
    /// </summary>
    [MessageLogging(EventId = 21003, Level = LogLevel.Error, Message = "Authentication type not specified for PostgreSQL connection '{connectionName}'")]
    public static partial IGenericMessage AuthenticationTypeNotSpecified(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when registration of a connection type component fails.
    /// </summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Error, Message = "Registration failed for PostgreSQL connection '{connectionName}' (type: '{connectionType}', component: '{component}'): {error}")]
    public static partial IGenericMessage RegistrationFailed(ILogger logger, string connectionName, string connectionType, string component, string? error);

    // ═══════════════════════════════════════════════════════════════════════════
    // Trace-Level Factory Diagnostic Events (5292-5297)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traces entry into the factory Create method with generic configuration.
    /// </summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace, Message = "Entering PostgreSqlConnectionFactory.Create with IGenericConfiguration '{configurationType}'")]
    public static partial IGenericMessage TraceFactoryCreateGenericEntry(ILogger logger, string configurationType);

    /// <summary>
    /// Traces entry into the factory Create method with typed configuration.
    /// </summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Trace, Message = "Entering PostgreSqlConnectionFactory.Create for connection '{connectionName}'")]
    public static partial IGenericMessage TraceFactoryCreateEntry(ILogger logger, string connectionName);

    /// <summary>
    /// Traces entry into the PostgreSqlConnection.Connect method.
    /// </summary>
    [MessageLogging(EventId = 11012, Level = LogLevel.Trace, Message = "Entering PostgreSqlConnection.Connect for '{connectionName}'")]
    public static partial IGenericMessage TraceConnectEntry(ILogger logger, string connectionName);

    /// <summary>
    /// Traces entry into the PostgreSqlConnection.Disconnect method.
    /// </summary>
    [MessageLogging(EventId = 11013, Level = LogLevel.Trace, Message = "Entering PostgreSqlConnection.Disconnect for '{connectionName}'")]
    public static partial IGenericMessage TraceDisconnectEntry(ILogger logger, string connectionName);

    /// <summary>
    /// Traces entry into the PostgreSqlConnection.Execute method.
    /// </summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Trace, Message = "Entering PostgreSqlConnection.Execute for '{connectionName}'")]
    public static partial IGenericMessage TraceExecuteEntry(ILogger logger, string connectionName);

    /// <summary>
    /// Traces factory Create with connection type specification.
    /// </summary>
    [MessageLogging(EventId = 11015, Level = LogLevel.Trace, Message = "Entering PostgreSqlConnectionFactory.Create with connectionType '{connectionType}'")]
    public static partial IGenericMessage TraceFactoryCreateWithTypeEntry(ILogger logger, string connectionType);

    /// <summary>
    /// Traces the SQL command text and parameter count immediately before execution.
    /// </summary>
    [MessageLogging(EventId = 11016, Level = LogLevel.Trace, Message = "Executing PostgreSQL command: {commandText} ({parameterCount} parameters)")]
    public static partial IGenericMessage ExecutingSqlCommand(ILogger logger, string commandText, int parameterCount);

    /// <summary>
    /// Logs successful execution of a SQL command with row count.
    /// </summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Information, Message = "PostgreSQL command executed: {commandText} ({rowsAffected} rows)")]
    public static partial IGenericMessage SqlCommandExecuted(ILogger logger, string commandText, int rowsAffected);

    /// <summary>
    /// Logs when a POCO mapper is not found for a type during result materialization.
    /// </summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Error, Message = "No POCO mapper found for type '{typeName}' ({fullTypeName}). Ensure [GenerateMapper] is applied.")]
    public static partial IGenericMessage NoMapperFound(ILogger logger, string typeName, string fullTypeName);

    /// <summary>
    /// Logs when POCO mapper fails to map a reader row to the target type.
    /// The mapper's own result message is already logged by the generated mapper code.
    /// </summary>
    [MessageLogging(EventId = 71005, Level = LogLevel.Error, Message = "Mapping failed for type '{typeName}' during PostgreSQL materialization")]
    public static partial IGenericMessage MappingFailed(ILogger logger, string typeName);

    /// <summary>
    /// Logs an exception that occurred during SQL command execution.
    /// </summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Error, Message = "PostgreSQL execution failed for command '{commandText}'")]
    public static partial IGenericMessage SqlExecutionFailed(ILogger logger, Exception ex, string commandText);

    // ═══════════════════════════════════════════════════════════════════════════
    // Health Probe Events (ISupportsHealthProbe)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traces entry into the PostgreSqlConnection.Probe method.
    /// </summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Trace, Message = "Entering PostgreSqlConnection.Probe for '{connectionName}'")]
    public static partial IGenericMessage TraceProbeEntry(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when a health probe (SELECT 1) succeeds.
    /// </summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Debug, Message = "Health probe succeeded for connection '{connectionName}'")]
    public static partial IGenericMessage ProbeSucceeded(ILogger logger, string connectionName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Bulk COPY Events (BulkInsert marker path)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs the start of a PostgreSQL binary COPY bulk-insert operation.
    /// </summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Debug, Message = "Starting PostgreSQL binary COPY for '{connectionName}' ({columnCount} columns)")]
    public static partial IGenericMessage BulkCopyStarting(ILogger logger, string connectionName, int columnCount);

    /// <summary>
    /// Logs a missing or invalid bulk-copy metadata parameter on the marker command.
    /// </summary>
    [MessageLogging(EventId = 71007, Level = LogLevel.Error, Message = "PostgreSQL bulk COPY metadata parameter '{parameterName}' missing or invalid for '{connectionName}'")]
    public static partial IGenericMessage BulkCopyMetadataMissing(ILogger logger, string connectionName, string parameterName);

    /// <summary>
    /// Logs an exception that occurred during a PostgreSQL binary COPY bulk-insert operation.
    /// </summary>
    [MessageLogging(EventId = 71008, Level = LogLevel.Error, Message = "PostgreSQL bulk COPY failed for '{connectionName}'")]
    public static partial IGenericMessage BulkCopyFailed(ILogger logger, Exception ex, string connectionName);
}
