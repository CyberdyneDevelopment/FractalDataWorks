using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.Logging;

/// <summary>
/// Static logger class for MsSql connection operations using MessageLogging infrastructure.
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlConnectionLogger
{
    /// <summary>
    /// Logs when SQL connection fails with details.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="details">The error details.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error, Message = "SQL connection failed with details: {details}")]
    public static partial IGenericMessage ConnectionFailedWithDetails(ILogger logger, string details);

    /// <summary>
    /// Logs when SQL execution fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(EventId = 90001, Level = LogLevel.Error, Message = "SQL execution failed")]
    public static partial IGenericMessage SqlExecutionFailed(ILogger logger);

    /// <summary>
    /// Logs when executing SQL command.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="commandText">The SQL command text.</param>
    /// <param name="parameterCount">The number of parameters.</param>
    /// <returns>A generic message containing the information.</returns>
    [MessageLogging(EventId = 11014, Level = LogLevel.Debug, Message = "Executing SQL command: {commandText} with {parameterCount} parameters")]
    public static partial IGenericMessage ExecutingSqlCommand(ILogger logger, string commandText, int parameterCount);

    /// <summary>
    /// Logs when SQL command is executed successfully.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="commandText">The SQL command text.</param>
    /// <param name="rowsAffected">The number of rows affected.</param>
    /// <returns>A generic message containing the information.</returns>
    [MessageLogging(EventId = 11015, Level = LogLevel.Debug, Message = "SQL command executed: {commandText}, {rowsAffected} rows affected")]
    public static partial IGenericMessage SqlCommandExecuted(ILogger logger, string commandText, int rowsAffected);

    /// <summary>
    /// Logs when SQL execution encounters an error.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="commandText">The SQL command text.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errorNumber">The SQL error number.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error, Message = "SQL execution error on '{commandText}': {errorMessage} (Error {errorNumber})")]
    public static partial IGenericMessage SqlExecutionError(ILogger logger, string commandText, string errorMessage, int errorNumber);

    /// <summary>
    /// Logs when an execution exception occurs.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="commandText">The SQL command text.</param>
    /// <param name="exceptionMessage">The exception message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(EventId = 71003, Level = LogLevel.Error, Message = "Execution exception on '{commandText}': {exceptionMessage}")]
    public static partial IGenericMessage ExecutionException(ILogger logger, string commandText, string exceptionMessage);

    /// <summary>
    /// Logs when opening a connection.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionType">The connection type.</param>
    /// <returns>A generic message containing the information.</returns>
    [MessageLogging(EventId = 11016, Level = LogLevel.Debug, Message = "Opening connection: {connectionType}")]
    public static partial IGenericMessage OpeningConnection(ILogger logger, string connectionType);

    /// <summary>
    /// Logs when connection is successfully opened.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionType">The connection type.</param>
    /// <returns>A generic message containing the information.</returns>
    [MessageLogging(EventId = 11017, Level = LogLevel.Information, Message = "Connection opened successfully: {connectionType}")]
    public static partial IGenericMessage ConnectionOpened(ILogger logger, string connectionType);

    /// <summary>
    /// Logs when connection fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionType">The connection type.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(EventId = 70000, Level = LogLevel.Error, Message = "Connection failed for {connectionType}: {errorMessage}")]
    public static partial IGenericMessage ConnectionFailed(ILogger logger, string connectionType, string errorMessage);

    /// <summary>
    /// Logs when closing a connection.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionType">The connection type.</param>
    /// <returns>A generic message containing the information.</returns>
    [MessageLogging(EventId = 11018, Level = LogLevel.Debug, Message = "Closing connection: {connectionType}")]
    public static partial IGenericMessage ClosingConnection(ILogger logger, string connectionType);

    /// <summary>
    /// Logs when connection is successfully closed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionType">The connection type.</param>
    /// <returns>A generic message containing the information.</returns>
    [MessageLogging(EventId = 11019, Level = LogLevel.Information, Message = "Connection closed successfully: {connectionType}")]
    public static partial IGenericMessage ConnectionClosed(ILogger logger, string connectionType);

    /// <summary>
    /// Logs when disconnection fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionType">The connection type.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(EventId = 71004, Level = LogLevel.Warning, Message = "Disconnection failed for {connectionType}: {errorMessage}")]
    public static partial IGenericMessage DisconnectionFailed(ILogger logger, string connectionType, string errorMessage);

    /// <summary>
    /// Logs when no POCO mapper is found for a type.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="typeName">The type name.</param>
    /// <param name="typeFullName">The full type name.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 31002,
        Level = LogLevel.Error,
        Message = "No POCO mapper found for type '{typeName}' (full name: '{typeFullName}'). Add [GenerateMapper] attribute to the type or create a manual PocoMapperBase implementation.")]
    public static partial IGenericMessage NoMapperFound(ILogger logger, string typeName, string typeFullName);

    /// <summary>
    /// Logs when POCO mapping fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="typeName">The type name.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 90002,
        Level = LogLevel.Error,
        Message = "Failed to map type '{typeName}': {errorMessage}")]
    public static partial IGenericMessage MappingFailed(ILogger logger, string typeName, string errorMessage);

    /// <summary>
    /// Logs when SQL execution fails with the caught exception.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="ex">The caught exception.</param>
    /// <param name="commandText">The SQL command text that failed.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71005,
        Level = LogLevel.Error,
        Message = "SQL execution failed for command: {commandText}")]
    public static partial IGenericMessage SqlExecutionFailedWithMessage(ILogger logger, Exception ex, string commandText);

    /// <summary>
    /// Logs when execution fails generically.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71006,
        Level = LogLevel.Error,
        Message = "Execution failed")]
    public static partial IGenericMessage ExecutionFailed(ILogger logger);

    /// <summary>
    /// Logs when bulk copy operation fails with the caught exception.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="ex">The caught exception.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 70001,
        Level = LogLevel.Error,
        Message = "Bulk copy operation failed")]
    public static partial IGenericMessage BulkCopyFailed(ILogger logger, Exception ex);

    /// <summary>
    /// Logs when connection to SQL Server fails with the caught exception.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="ex">The caught exception.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71007,
        Level = LogLevel.Error,
        Message = "Failed to connect to SQL Server")]
    public static partial IGenericMessage ConnectFailed(ILogger logger, Exception ex);

    /// <summary>
    /// Logs when disconnection from SQL Server fails with the caught exception.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="ex">The caught exception.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71008,
        Level = LogLevel.Error,
        Message = "Failed to disconnect from SQL Server")]
    public static partial IGenericMessage DisconnectFailed(ILogger logger, Exception ex);

    /// <summary>
    /// Logs when discovery capability test fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71009,
        Level = LogLevel.Error,
        Message = "Discovery capability test failed: {errorMessage}")]
    public static partial IGenericMessage DiscoveryTestFailed(ILogger logger, string errorMessage);

    /// <summary>
    /// Logs when schema discovery fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71010,
        Level = LogLevel.Error,
        Message = "Failed to discover schemas: {errorMessage}")]
    public static partial IGenericMessage SchemaDiscoveryFailed(ILogger logger, string errorMessage);

    /// <summary>
    /// Logs when container discovery fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71011,
        Level = LogLevel.Error,
        Message = "Failed to discover containers in schema '{schemaName}': {errorMessage}")]
    public static partial IGenericMessage ContainerDiscoveryInSchemaFailed(ILogger logger, string schemaName, string errorMessage);

    /// <summary>
    /// Logs when column discovery fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="objectName">The object name.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71012,
        Level = LogLevel.Error,
        Message = "Failed to discover columns for '{schemaName}.{objectName}': {errorMessage}")]
    public static partial IGenericMessage ColumnDiscoveryFailed(ILogger logger, string schemaName, string objectName, string errorMessage);

    /// <summary>
    /// Logs when container discovery fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="objectName">The object name.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71013,
        Level = LogLevel.Error,
        Message = "Failed to discover container '{schemaName}.{objectName}': {errorMessage}")]
    public static partial IGenericMessage ContainerObjectDiscoveryFailed(ILogger logger, string schemaName, string objectName, string errorMessage);

    /// <summary>
    /// Logs when primary key discovery fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71014,
        Level = LogLevel.Error,
        Message = "Failed to discover primary key: {errorMessage}")]
    public static partial IGenericMessage PrimaryKeyDiscoveryFailed(ILogger logger, string errorMessage);

    /// <summary>
    /// Logs when index discovery fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71015,
        Level = LogLevel.Error,
        Message = "Failed to discover indexes: {errorMessage}")]
    public static partial IGenericMessage IndexDiscoveryFailed(ILogger logger, string errorMessage);

    /// <summary>
    /// Logs when object is not found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="objectName">The object name.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 31003,
        Level = LogLevel.Error,
        Message = "Object '{schemaName}.{objectName}' not found")]
    public static partial IGenericMessage ObjectNotFound(ILogger logger, string schemaName, string objectName);

    /// <summary>
    /// Logs when authentication validation fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 51002,
        Level = LogLevel.Error,
        Message = "Authentication validation failed")]
    public static partial IGenericMessage AuthValidationFailed(ILogger logger);

    /// <summary>
    /// Logs when authentication processing fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 51003,
        Level = LogLevel.Error,
        Message = "Failed to process authentication")]
    public static partial IGenericMessage AuthProcessingFailed(ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // SQL Error Handler Events (3020-3026)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a SQL permission denied error (229) occurs.
    /// </summary>
    [MessageLogging(EventId = 50001, Level = LogLevel.Error, Message = "SQL permission denied on '{commandText}'")]
    public static partial IGenericMessage PermissionDenied(ILogger logger, Exception ex, string commandText);

    /// <summary>
    /// Logs when a SQL object is not found (error 208).
    /// </summary>
    [MessageLogging(EventId = 30000, Level = LogLevel.Error, Message = "SQL object not found on '{commandText}'")]
    public static partial IGenericMessage SqlObjectNotFound(ILogger logger, Exception ex, string commandText);

    /// <summary>
    /// Logs when a SQL login fails (error 18456).
    /// </summary>
    [MessageLogging(EventId = 51004, Level = LogLevel.Error, Message = "SQL login failed on '{commandText}'")]
    public static partial IGenericMessage SqlLoginFailed(ILogger logger, Exception ex, string commandText);

    /// <summary>
    /// Logs when a SQL Server instance is unreachable (errors -1, 2, 53).
    /// </summary>
    [MessageLogging(EventId = 71016, Level = LogLevel.Error, Message = "SQL Server unreachable on '{commandText}'")]
    public static partial IGenericMessage SqlServerUnreachable(ILogger logger, Exception ex, string commandText);

    /// <summary>
    /// Logs when a SQL deadlock occurs (error 1205).
    /// </summary>
    [MessageLogging(EventId = 81000, Level = LogLevel.Error, Message = "SQL deadlock on '{commandText}'")]
    public static partial IGenericMessage SqlDeadlock(ILogger logger, Exception ex, string commandText);

    /// <summary>
    /// Logs when a SQL query timeout occurs (error -2).
    /// </summary>
    [MessageLogging(EventId = 81001, Level = LogLevel.Error, Message = "SQL query timeout on '{commandText}'")]
    public static partial IGenericMessage SqlQueryTimeout(ILogger logger, Exception ex, string commandText);

    /// <summary>
    /// Logs an unhandled SQL error with full context for diagnosis.
    /// </summary>
    [MessageLogging(EventId = 71017, Level = LogLevel.Error, Message = "Unhandled SQL error {errorNumber} on '{commandText}': {errorMessage}")]
    public static partial IGenericMessage UnhandledSqlError(ILogger logger, Exception ex, string commandText, int errorNumber, string errorMessage);

    /// <summary>
    /// Logs when a connection is stale (disposed) and needs to be recreated from the provider.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionName">The name of the stale connection.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(EventId = 41000, Level = LogLevel.Error, Message = "Connection '{connectionName}' is stale (disposed) — recreate from provider")]
    public static partial IGenericMessage ConnectionStale(ILogger logger, string connectionName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Trace-Level Connection Diagnostic Events (3015-3018)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traces entry into the MsSqlConnection.Execute method.
    /// </summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Trace, Message = "Entering MsSqlConnection.Execute for '{commandText}'")]
    public static partial IGenericMessage TraceExecuteEntry(ILogger logger, string commandText);

    /// <summary>
    /// Traces entry into the MsSqlConnection.Connect method with target details.
    /// </summary>
    [MessageLogging(EventId = 11021, Level = LogLevel.Trace, Message = "MsSqlConnection.Connect opening connection to '{server}' database '{database}'")]
    public static partial IGenericMessage TraceConnectEntry(ILogger logger, string server, string database);

    /// <summary>
    /// Traces the actual connection details after SqlConnection.OpenAsync completes.
    /// DataSource and Database are resolved by the driver after the TCP handshake.
    /// </summary>
    [MessageLogging(EventId = 11022, Level = LogLevel.Trace, Message = "MsSqlConnection opened: DataSource='{dataSource}', Database='{database}'")]
    public static partial IGenericMessage TraceConnectionOpened(ILogger logger, string dataSource, string database);

    /// <summary>
    /// Traces entry into the MsSqlConnection.Disconnect method.
    /// </summary>
    [MessageLogging(EventId = 11023, Level = LogLevel.Trace, Message = "Entering MsSqlConnection.Disconnect")]
    public static partial IGenericMessage TraceDisconnectEntry(ILogger logger);

    /// <summary>
    /// Traces entry into the MsSqlConnection.TestConnection method.
    /// </summary>
    [MessageLogging(EventId = 11024, Level = LogLevel.Trace, Message = "Entering MsSqlConnection.TestConnection")]
    public static partial IGenericMessage TraceTestConnectionEntry(ILogger logger);

    /// <summary>
    /// Traces when tenant SESSION_CONTEXT is set on a pooled connection.
    /// </summary>
    [MessageLogging(EventId = 11025, Level = LogLevel.Trace, Message = "Set SESSION_CONTEXT TenantId='{tenantId}' on pooled connection")]
    public static partial IGenericMessage TraceTenantContextSet(ILogger logger, string tenantId);

    /// <summary>
    /// Traces when a pooled connection is used under an explicit system-elevation context — NO
    /// SESSION_CONTEXT keys are set at all (the resulting NULL UserId is what
    /// security.fn_TenantFilter's Mode 1 bypass checks for; there is no dedicated SystemContext key).
    /// </summary>
    [MessageLogging(EventId = 11034, Level = LogLevel.Trace, Message = "System-elevated connection used — no SESSION_CONTEXT keys set on pooled connection")]
    public static partial IGenericMessage TraceSystemBypassConnectionUsed(ILogger logger);

    /// <summary>
    /// Traces when SESSION_CONTEXT('UserId') is set to the reserved deny-everywhere
    /// NoAccessPrincipalId — no established, Guid-identified, or system-elevated context was
    /// available for this connection, so it is denied tenant-scoped visibility (sees only
    /// shared/system rows).
    /// </summary>
    [MessageLogging(EventId = 11067, Level = LogLevel.Trace, Message = "Set SESSION_CONTEXT UserId to the reserved NoAccessPrincipalId (deny-everywhere) on pooled connection")]
    public static partial IGenericMessage TraceNoAccessPrincipalContextSet(ILogger logger);

    /// <summary>
    /// Logs when setting tenant SESSION_CONTEXT fails.
    /// </summary>
    [MessageLogging(EventId = 71018, Level = LogLevel.Error, Message = "Failed to set tenant SESSION_CONTEXT: {errorMessage}")]
    public static partial IGenericMessage TenantContextSetFailed(ILogger logger, string errorMessage);

    /// <summary>
    /// Traces when cross-tenant SESSION_CONTEXT is set on a pooled connection (CrossTenant='1').
    /// </summary>
    [MessageLogging(EventId = 11026, Level = LogLevel.Trace, Message = "Set SESSION_CONTEXT CrossTenant='1' for userId='{userId}' on pooled connection")]
    public static partial IGenericMessage TraceCrossTenantContextSet(ILogger logger, string userId);

    /// <summary>
    /// Traces when SESSION_CONTEXT CanReadSecrets='1' is set because the token carries
    /// the <c>connections:read-secrets</c> permission. Never logs the permission list.
    /// </summary>
    [MessageLogging(EventId = 11027, Level = LogLevel.Trace, Message = "Set SESSION_CONTEXT CanReadSecrets='1' on pooled connection")]
    public static partial IGenericMessage TraceCanReadSecretsContextSet(ILogger logger);

    // Why: 3030 was AmbiguousTypedBodyParent — removed with TypedBodyParent elimination (FDW-479).
    // Keeping the EventId gap so existing log archives remain readable.

    // ═══════════════════════════════════════════════════════════════════════════
    // Transaction Events (5231-5235)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traces when a transaction is started on a pooled connection.
    /// </summary>
    [MessageLogging(EventId = 11028, Level = LogLevel.Trace, Message = "Transaction started on connection '{connectionName}'")]
    public static partial IGenericMessage TransactionStarted(ILogger logger, string connectionName);

    /// <summary>
    /// Traces when a transaction is committed.
    /// </summary>
    [MessageLogging(EventId = 11029, Level = LogLevel.Trace, Message = "Transaction committed on connection '{connectionName}'")]
    public static partial IGenericMessage TransactionCommitted(ILogger logger, string connectionName);

    /// <summary>
    /// Traces when a transaction is rolled back.
    /// </summary>
    [MessageLogging(EventId = 11030, Level = LogLevel.Trace, Message = "Transaction rolled back on connection '{connectionName}'")]
    public static partial IGenericMessage TransactionRolledBack(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when starting a transaction fails.
    /// </summary>
    [MessageLogging(EventId = 71019, Level = LogLevel.Error, Message = "Failed to start transaction on connection '{connectionName}': {errorMessage}")]
    public static partial IGenericMessage TransactionStartFailed(ILogger logger, string connectionName, string errorMessage);

    /// <summary>
    /// Logs when committing a transaction fails.
    /// </summary>
    [MessageLogging(EventId = 71020, Level = LogLevel.Error, Message = "Failed to commit transaction on connection '{connectionName}': {errorMessage}")]
    public static partial IGenericMessage TransactionCommitFailed(ILogger logger, string connectionName, string errorMessage);

    /// <summary>
    /// Logs when rolling back a transaction fails.
    /// </summary>
    [MessageLogging(EventId = 71021, Level = LogLevel.Error, Message = "Failed to roll back transaction on connection '{connectionName}': {errorMessage}")]
    public static partial IGenericMessage TransactionRollbackFailed(ILogger logger, string connectionName, string errorMessage);

    /// <summary>
    /// Logs when the implicit rollback during dispose encounters an exception that is safely ignored.
    /// This occurs when the transaction is already in a terminal state (committed, rolled back, or
    /// the connection was broken) — SQL Server will have cleaned up on its end.
    /// </summary>
    [MessageLogging(EventId = 11031, Level = LogLevel.Debug, Message = "Implicit rollback on dispose ignored for connection '{connectionName}': {errorMessage}")]
    public static partial IGenericMessage TransactionDisposeRollbackIgnored(ILogger logger, Exception ex, string connectionName, string errorMessage);

    // ═══════════════════════════════════════════════════════════════════════════
    // Health Probe Events (ISupportsHealthProbe)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traces entry into the MsSqlConnection.Probe method.
    /// </summary>
    [MessageLogging(EventId = 11032, Level = LogLevel.Trace, Message = "Entering MsSqlConnection.Probe for connection '{connectionName}'")]
    public static partial IGenericMessage TraceProbeEntry(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when a health probe (SELECT 1) succeeds.
    /// </summary>
    // Why Debug, not Information: a successful periodic probe is steady-state noise — it fired every
    // 5m15s forever. The probe FAILURE record keeps its own higher level.
    [MessageLogging(EventId = 11033, Level = LogLevel.Debug, Message = "Health probe succeeded for connection '{connectionName}'")]
    public static partial IGenericMessage ProbeSucceeded(ILogger logger, string connectionName);
}
