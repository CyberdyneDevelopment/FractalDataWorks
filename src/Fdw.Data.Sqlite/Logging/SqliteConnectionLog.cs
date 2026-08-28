using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Sqlite.Logging;

/// <summary>
/// MessageLogging for SQLite connection operations.
/// EventId range: 9727-9757
/// </summary>
[MessageLoggingTypeCode("SQLITE")]
public static partial class SqliteConnectionLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Trace
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when building a SQLite connection string.
    /// </summary>
    [MessageLogging(EventId = 9727, Level = LogLevel.Trace, Message = "Building SQLite connection string for '{connectionName}'")]
    public static partial IGenericMessage BuildingConnectionString(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when resolving a secret manager for password retrieval.
    /// </summary>
    [MessageLogging(EventId = 9728, Level = LogLevel.Trace, Message = "Resolving secret manager '{secretManagerName}' for SQLite connection '{connectionName}'")]
    public static partial IGenericMessage ResolvingSecretManager(ILogger logger, string secretManagerName, string connectionName);

    /// <summary>
    /// Logs when entering the factory Create method.
    /// </summary>
    [MessageLogging(EventId = 9729, Level = LogLevel.Trace, Message = "Entering SqliteConnectionFactory.Create for '{connectionName}'")]
    public static partial IGenericMessage TraceFactoryCreateEntry(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when entering the Execute method.
    /// </summary>
    [MessageLogging(EventId = 9730, Level = LogLevel.Trace, Message = "Entering SqliteConnection.Execute for '{connectionName}'")]
    public static partial IGenericMessage TraceExecuteEntry(ILogger logger, string connectionName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Debug
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs resolved SQLite configuration details.
    /// </summary>
    [MessageLogging(EventId = 9731, Level = LogLevel.Debug, Message = "SQLite configuration resolved for '{connectionName}': DataSource={dataSource}, Mode={mode}")]
    public static partial IGenericMessage ConfigurationResolved(ILogger logger, string connectionName, string dataSource, string mode);

    /// <summary>
    /// Logs when a SQLite command is being executed.
    /// </summary>
    [MessageLogging(EventId = 9732, Level = LogLevel.Debug, Message = "Executing SQLite command on '{connectionName}': {commandText} ({parameterCount} params)")]
    public static partial IGenericMessage ExecutingCommand(ILogger logger, string connectionName, string commandText, int parameterCount);

    /// <summary>
    /// Logs when a SQLite command completes.
    /// </summary>
    [MessageLogging(EventId = 9733, Level = LogLevel.Debug, Message = "SQLite command completed on '{connectionName}': {rowsAffected} rows affected")]
    public static partial IGenericMessage CommandCompleted(ILogger logger, string connectionName, int rowsAffected);

    /// <summary>
    /// Logs the start of a transaction.
    /// </summary>
    [MessageLogging(EventId = 9734, Level = LogLevel.Debug, Message = "SQLite transaction begun on '{connectionName}'")]
    public static partial IGenericMessage TransactionBegan(ILogger logger, string connectionName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Information
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a SQLite connection is successfully opened.
    /// </summary>
    [MessageLogging(EventId = 9735, Level = LogLevel.Information, Message = "SQLite connection opened for '{connectionName}': {dataSource}")]
    public static partial IGenericMessage ConnectionOpened(ILogger logger, string connectionName, string dataSource);

    /// <summary>
    /// Logs when a SQLite connection is closed.
    /// </summary>
    [MessageLogging(EventId = 9736, Level = LogLevel.Information, Message = "SQLite connection closed for '{connectionName}'")]
    public static partial IGenericMessage ConnectionClosed(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when a transaction is committed.
    /// </summary>
    [MessageLogging(EventId = 9737, Level = LogLevel.Information, Message = "SQLite transaction committed on '{connectionName}'")]
    public static partial IGenericMessage TransactionCommitted(ILogger logger, string connectionName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Warning
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a transaction is rolled back.
    /// </summary>
    [MessageLogging(EventId = 9738, Level = LogLevel.Warning, Message = "SQLite transaction rolled back on '{connectionName}': {reason}")]
    public static partial IGenericMessage TransactionRolledBack(ILogger logger, string connectionName, string reason);

    /// <summary>
    /// Logs when disconnection fails cleanly.
    /// </summary>
    [MessageLogging(EventId = 9739, Level = LogLevel.Warning, Message = "SQLite disconnection failed for '{connectionName}': {errorMessage}")]
    public static partial IGenericMessage DisconnectionFailed(ILogger logger, string connectionName, string errorMessage);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a SQLite connection attempt fails.
    /// </summary>
    [MessageLogging(EventId = 9740, Level = LogLevel.Error, Message = "SQLite connection failed for '{connectionName}': {errorMessage}")]
    public static partial IGenericMessage ConnectionFailed(ILogger logger, string connectionName, string errorMessage);

    /// <summary>
    /// Logs when the factory receives null configuration.
    /// </summary>
    [MessageLogging(EventId = 9741, Level = LogLevel.Error, Message = "SqliteConnectionFactory received null configuration")]
    public static partial IGenericMessage ConfigurationNull(ILogger logger);

    /// <summary>
    /// Logs when an invalid configuration type is passed to the factory.
    /// </summary>
    [MessageLogging(EventId = 9742, Level = LogLevel.Error, Message = "Invalid configuration type for SQLite factory. Expected SqliteConnectionConfiguration, got '{actualType}'")]
    public static partial IGenericMessage InvalidConfigurationType(ILogger logger, string actualType);

    /// <summary>
    /// Logs when SQLite connection creation fails.
    /// </summary>
    [MessageLogging(EventId = 9743, Level = LogLevel.Error, Message = "Failed to create SQLite connection for '{connectionName}': {errorMessage}")]
    public static partial IGenericMessage CreationFailed(ILogger logger, string connectionName, string errorMessage);

    /// <summary>
    /// Logs when a secret manager is not found by name.
    /// </summary>
    [MessageLogging(EventId = 9744, Level = LogLevel.Error, Message = "Secret manager '{secretManagerName}' not found for SQLite connection '{connectionName}'")]
    public static partial IGenericMessage SecretManagerNotFound(ILogger logger, string connectionName, string secretManagerName);

    /// <summary>
    /// Logs when a secret key is not found in the secret manager.
    /// </summary>
    [MessageLogging(EventId = 9745, Level = LogLevel.Error, Message = "Secret '{secretKeyName}' not found for SQLite connection '{connectionName}'")]
    public static partial IGenericMessage SecretNotFound(ILogger logger, string connectionName, string secretKeyName);

    /// <summary>
    /// Logs when no POCO mapper is found for a type.
    /// </summary>
    [MessageLogging(EventId = 9746, Level = LogLevel.Error, Message = "No POCO mapper found for type '{typeName}' ({typeFullName}). Add [GenerateMapper] to the type.")]
    public static partial IGenericMessage NoMapperFound(ILogger logger, string typeName, string typeFullName);

    /// <summary>
    /// Logs when POCO mapping fails.
    /// </summary>
    [MessageLogging(EventId = 9747, Level = LogLevel.Error, Message = "Failed to map type '{typeName}': {errorMessage}")]
    public static partial IGenericMessage MappingFailed(ILogger logger, string typeName, string errorMessage);

    /// <summary>
    /// Logs when SQL execution fails.
    /// </summary>
    [MessageLogging(EventId = 9748, Level = LogLevel.Error, Message = "SQLite execution failed on '{connectionName}': {errorMessage}")]
    public static partial IGenericMessage ExecutionFailed(ILogger logger, string connectionName, string errorMessage);

    /// <summary>
    /// Logs when the DataSource path is missing or empty.
    /// </summary>
    [MessageLogging(EventId = 9749, Level = LogLevel.Error, Message = "SQLite DataSource is missing or empty for connection '{connectionName}' — cannot create connection string")]
    public static partial IGenericMessage DataSourceMissing(ILogger logger, string connectionName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Trace — transaction operation entry points
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when entering Execute on a transaction.
    /// </summary>
    [MessageLogging(EventId = 9755, Level = LogLevel.Trace, Message = "Entering SqliteDataConnectionTransaction.Execute for '{connectionName}'")]
    public static partial IGenericMessage TraceTransactionExecuteEntry(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when entering Commit on a transaction.
    /// </summary>
    [MessageLogging(EventId = 9756, Level = LogLevel.Trace, Message = "Entering SqliteDataConnectionTransaction.Commit for '{connectionName}'")]
    public static partial IGenericMessage TraceTransactionCommitEntry(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when entering Rollback on a transaction.
    /// </summary>
    [MessageLogging(EventId = 9757, Level = LogLevel.Trace, Message = "Entering SqliteDataConnectionTransaction.Rollback for '{connectionName}'")]
    public static partial IGenericMessage TraceTransactionRollbackEntry(ILogger logger, string connectionName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Warning — exception-carrying
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when the implicit rollback in DisposeAsync fails (log-and-swallow: disposal must not throw).
    /// </summary>
    [MessageLogging(EventId = 9750, Level = LogLevel.Warning, Message = "SQLite implicit rollback during DisposeAsync failed for '{connectionName}' — connection may have already been closed or the transaction already completed")]
    public static partial IGenericMessage DisposeRollbackFailed(ILogger logger, Exception ex, string connectionName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error — exception-carrying variants (preserve full stack/inner exception)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when SQL execution fails, chaining the caught exception for full stack capture.
    /// </summary>
    [MessageLogging(EventId = 9751, Level = LogLevel.Error, Message = "SQLite execution failed on '{connectionName}'")]
    public static partial IGenericMessage ExecutionFailedWithException(ILogger logger, Exception ex, string connectionName);

    /// <summary>
    /// Logs when opening a connection fails, chaining the caught exception.
    /// </summary>
    [MessageLogging(EventId = 9752, Level = LogLevel.Error, Message = "SQLite connection failed for '{connectionName}'")]
    public static partial IGenericMessage ConnectionFailedWithException(ILogger logger, Exception ex, string connectionName);

    /// <summary>
    /// Logs when connection creation fails, chaining the caught exception.
    /// </summary>
    [MessageLogging(EventId = 9753, Level = LogLevel.Error, Message = "Failed to create SQLite connection for '{connectionName}'")]
    public static partial IGenericMessage CreationFailedWithException(ILogger logger, Exception ex, string connectionName);

    /// <summary>
    /// Logs when a translator catch block fires, chaining the caught exception.
    /// Called from translator catch blocks (which use NullLogger — ensures the exception is
    /// formally observed even when no live logger is wired to the translator).
    /// </summary>
    [MessageLogging(EventId = 9754, Level = LogLevel.Error, Message = "SQLite {translatorName} translator failed: {errorMessage}")]
    public static partial IGenericMessage TranslationFailed(ILogger logger, Exception ex, string translatorName, string errorMessage);

    // ═══════════════════════════════════════════════════════════════════════════
    // Health Probe Events (ISupportsHealthProbe)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traces entry into the SqliteConnection.Probe method.
    /// </summary>
    [MessageLogging(EventId = 9758, Level = LogLevel.Trace, Message = "Entering SqliteConnection.Probe for '{connectionName}'")]
    public static partial IGenericMessage TraceProbeEntry(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when a health probe (SELECT 1) succeeds.
    /// </summary>
    [MessageLogging(EventId = 9759, Level = LogLevel.Debug, Message = "Health probe succeeded for connection '{connectionName}'")]
    public static partial IGenericMessage ProbeSucceeded(ILogger logger, string connectionName);
}
