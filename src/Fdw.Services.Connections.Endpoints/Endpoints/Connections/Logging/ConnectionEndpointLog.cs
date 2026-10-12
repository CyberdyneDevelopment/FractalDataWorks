using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints.Logging;

/// <summary>
/// Message logging for connection endpoint operations.
/// EventId range: 7119-7149
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS6")]
public static partial class ConnectionEndpointLog
{
    /// <summary>Logs when the optional connection setup step fails during connection creation.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Warning, Message = "Connection setup failed for '{connectionName}': {error}")]
    public static partial IGenericMessage ConnectionSetupFailed(ILogger logger, string connectionName, string error);

    /// <summary>Logs when a capabilities request is made for an unknown connection type.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning, Message = "Connection type '{connectionTypeName}' not found when resolving capabilities.")]
    public static partial IGenericMessage ConnectionTypeNotFound(ILogger logger, string connectionTypeName);

    /// <summary>Logs when capabilities are successfully resolved for a connection type.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace, Message = "Resolved capabilities for connection type '{connectionTypeName}'.")]
    public static partial IGenericMessage CapabilitiesResolved(ILogger logger, string connectionTypeName);

    /// <summary>Logs when an in-memory connection configuration test is starting.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace, Message = "Testing in-memory connection config for '{connectionName}'")]
    public static partial IGenericMessage TestingConnectionConfig(ILogger logger, string connectionName);

    /// <summary>Logs when an in-memory connection configuration test succeeded.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information, Message = "Connection config test succeeded for '{connectionName}'")]
    public static partial IGenericMessage ConnectionConfigTestSucceeded(ILogger logger, string connectionName);

    /// <summary>Logs when an in-memory connection configuration test failed.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Warning, Message = "Connection config test failed for '{connectionName}': {message}")]
    public static partial IGenericMessage ConnectionConfigTestFailed(ILogger logger, string connectionName, string message);

    /// <summary>Logs when building a connection from configuration failed.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Failed to build connection from config for '{connectionName}': {message}")]
    public static partial IGenericMessage ConnectionConfigBuildFailed(ILogger logger, string connectionName, string message);

    /// <summary>Logs when a modification is rejected because the connection is a system configuration.</summary>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning, Message = "Rejected modification of system connection '{connectionName}' — system configurations are read-only")]
    public static partial IGenericMessage SystemConnectionReadOnly(ILogger logger, string connectionName);

    /// <summary>Logs when a health check is recorded for a connection.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace, Message = "Recorded health check for connection '{connectionName}': healthy={isHealthy}")]
    public static partial IGenericMessage HealthCheckRecorded(ILogger logger, string connectionName, bool isHealthy);

    /// <summary>Logs when recording a health check fails.</summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Warning, Message = "Failed to record health check for connection '{connectionName}': {error}")]
    public static partial IGenericMessage HealthCheckRecordFailed(ILogger logger, string connectionName, string error);

    /// <summary>Logs when a connection is not found while retrieving health history.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning, Message = "Connection '{connectionName}' not found when retrieving health history")]
    public static partial IGenericMessage HealthHistoryConnectionNotFound(ILogger logger, string connectionName);

    /// <summary>Logs when loading health history fails.</summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Warning, Message = "Failed to load health history for connection '{connectionName}': {error}")]
    public static partial IGenericMessage HealthHistoryLoadFailed(ILogger logger, string connectionName, string error);

    /// <summary>Logs when a connection configuration is not found during an endpoint operation.</summary>
    [MessageLogging(EventId = 31002, Level = LogLevel.Warning, Message = "Connection '{connectionName}' not found")]
    public static partial IGenericMessage ConnectionNotFound(ILogger logger, string connectionName);

    /// <summary>Logs when typed-body lookup is skipped during a delete (FDW013 failure-path coverage).</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Debug, Message = "Typed body lookup skipped during delete of connection '{connectionName}' (connectionId={connectionId}); proceeding with parent delete only")]
    public static partial IGenericMessage TypedBodyLookupSkipped(ILogger logger, string connectionName, System.Guid connectionId);

    /// <summary>Logs when a create/update request enables health checks without any trigger that would ever run them.</summary>
    [MessageLogging(EventId = 71005, Level = LogLevel.Warning, Message = "Connection '{connectionName}' has HealthCheckEnabled=true but no trigger is configured (HealthCheckOnStartup=false and HealthCheckIntervalSeconds is null) — this would never be checked; refusing to save")]
    public static partial IGenericMessage HealthCheckEnabledWithoutTrigger(ILogger logger, string connectionName);

    /// <summary>A request tried to choose where a connection's credentials are stored (FDW-1086).</summary>
    // Why Warning: the request is refused; a caller naming a secret is either a stale client or a probe.
    [MessageLogging(EventId = 21000, Level = LogLevel.Warning, Message = "Connection '{connectionName}' refused: the request set '{property}', which only the server sets")]
    public static partial IGenericMessage CredentialReferenceRefused(ILogger logger, string connectionName, string property);

    /// <summary>An update stated a member a connection does not store, so there was nothing for it to change (FDW-1177).</summary>
    // Why Warning: the request is refused; a caller stating it is a stale client that believes the member does something.
    [MessageLogging(EventId = 21001, Level = LogLevel.Warning, Message = "Connection '{connectionName}' update refused: it states '{property}', which a connection does not store")]
    public static partial IGenericMessage ConnectionMemberNotSettable(ILogger logger, string connectionName, string property);

    /// <summary>A connection's credentials were stored; the count only, never a value or a secret name.</summary>
    // Why Information: a credential change is an operator-visible event worth an audit line.
    [MessageLogging(EventId = 11005, Level = LogLevel.Information, Message = "Stored {count} credential(s) for connection '{connectionName}'")]
    public static partial IGenericMessage CredentialsStored(ILogger logger, string connectionName, int count);

    /// <summary>A secret a connection no longer names could not be deleted; it is never read again.</summary>
    // Why Warning: nothing is broken for the caller, but an orphaned secret is left for an operator to remove.
    [MessageLogging(EventId = 71006, Level = LogLevel.Warning, Message = "Secret '{secretKey}' that connection '{connectionName}' no longer names could not be deleted")]
    public static partial IGenericMessage CredentialRetireFailed(ILogger logger, string connectionName, string secretKey);

    /// <summary>A secret a connection names was not deleted because the credential store did not mint it; it belongs to whoever keeps it.</summary>
    // Why Information: this is the designed outcome for a secret an operator keeps, not a fault; the line records what was left.
    [MessageLogging(EventId = 71009, Level = LogLevel.Information, Message = "Secret '{secretKey}' in secret manager '{secretManagerName}' was left in place: the credential store did not create it for connection '{connectionName}'")]
    public static partial IGenericMessage CredentialLeftAlone(ILogger logger, string connectionName, string secretManagerName, string secretKey);

    /// <summary>The secret manager holding a connection's replaced credentials could not be opened to delete them.</summary>
    // Why Warning: the save succeeded; the old secrets are orphaned until an operator removes them.
    [MessageLogging(EventId = 71007, Level = LogLevel.Warning, Message = "Secret manager '{secretManagerName}' could not be opened to delete the secrets connection '{connectionName}' replaced")]
    public static partial IGenericMessage CredentialSecretManagerUnavailable(ILogger logger, string connectionName, string secretManagerName);

    /// <summary>The credential secret manager refused or failed to store one credential of a connection (FDW-1176); the field and the store's result code, never a value.</summary>
    // Why Error: the connection is not saved and nothing the caller can change fixes it; an operator must read the store's own log.
    [MessageLogging(EventId = 71008, Level = LogLevel.Error, Message = "Connection '{connectionName}' was not saved: the credential store failed to store '{field}' (store result {reason})")]
    public static partial IGenericMessage CredentialWriteFailed(ILogger logger, string connectionName, string field, string reason);

    /// <summary>The server setting naming the credential secret manager is empty (FDW-1086).</summary>
    // Why Error: no connection's credentials can be stored until an operator sets it.
    [MessageLogging(EventId = 61000, Level = LogLevel.Error, Message = "Connection credentials cannot be stored: the server setting '{setting}' names no secret manager")]
    public static partial IGenericMessage CredentialStoreNotConfigured(ILogger logger, string setting);
}
