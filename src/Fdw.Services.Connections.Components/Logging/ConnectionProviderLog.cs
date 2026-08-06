using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Connections.Components.Logging;

/// <summary>
/// MessageLogging methods for ConnectionProvider operations.
/// Provider-specific messages with domain context baked into templates.
/// EventId range: 8980-8999
/// </summary>
[MessageLoggingTypeCode("COMPONENTS8")]
public static partial class ConnectionProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Load Connections (8980-8981)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading the connections list fails.</summary>
    [MessageLogging(EventId = 71005, Level = LogLevel.Warning,
        Message = "ConnectionProvider: Failed to load connections list")]
    public static partial IGenericMessage LoadConnectionsFailed(
        ILogger logger);

    /// <summary>Logs when loading the connections list fails with exception.</summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Warning,
        Message = "ConnectionProvider: Failed to load connections list")]
    public static partial IGenericMessage LoadConnectionsException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Load Connection Types (8982-8983)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading the connection types list fails.</summary>
    [MessageLogging(EventId = 71007, Level = LogLevel.Warning,
        Message = "ConnectionProvider: Failed to load connection types list")]
    public static partial IGenericMessage LoadConnectionTypesFailed(
        ILogger logger);

    /// <summary>Logs when loading the connection types list fails with exception.</summary>
    [MessageLogging(EventId = 71008, Level = LogLevel.Warning,
        Message = "ConnectionProvider: Failed to load connection types list")]
    public static partial IGenericMessage LoadConnectionTypesException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Connection Detail (8984-8985)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading connection details fails.</summary>
    [MessageLogging(EventId = 71009, Level = LogLevel.Warning,
        Message = "ConnectionProvider: Failed to load connection detail for '{connectionName}'")]
    public static partial IGenericMessage ConnectionDetailLoadFailed(
        ILogger logger,
        string connectionName);

    /// <summary>Logs when loading connection details fails with exception.</summary>
    [MessageLogging(EventId = 71010, Level = LogLevel.Warning,
        Message = "ConnectionProvider: Failed to load connection detail")]
    public static partial IGenericMessage ConnectionDetailLoadException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Create Connection (8986-8987)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when creating a connection fails.</summary>
    [MessageLogging(EventId = 71011, Level = LogLevel.Warning,
        Message = "ConnectionProvider: Failed to create connection")]
    public static partial IGenericMessage ConnectionCreateFailed(
        ILogger logger);

    /// <summary>Logs when creating a connection fails with exception.</summary>
    [MessageLogging(EventId = 71012, Level = LogLevel.Warning,
        Message = "ConnectionProvider: Failed to create connection")]
    public static partial IGenericMessage ConnectionCreateException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Update Connection (8988-8989)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when updating a connection fails.</summary>
    [MessageLogging(EventId = 71013, Level = LogLevel.Warning,
        Message = "ConnectionProvider: Failed to update connection '{connectionName}'")]
    public static partial IGenericMessage ConnectionUpdateFailed(
        ILogger logger,
        string connectionName);

    /// <summary>Logs when updating a connection fails with exception.</summary>
    [MessageLogging(EventId = 71014, Level = LogLevel.Warning,
        Message = "ConnectionProvider: Failed to update connection")]
    public static partial IGenericMessage ConnectionUpdateException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Delete Connection (8990-8991)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when deleting a connection fails.</summary>
    [MessageLogging(EventId = 71015, Level = LogLevel.Warning,
        Message = "ConnectionProvider: Failed to delete connection '{connectionName}'")]
    public static partial IGenericMessage ConnectionDeleteFailed(
        ILogger logger,
        string connectionName);

    /// <summary>Logs when deleting a connection fails with exception.</summary>
    [MessageLogging(EventId = 71016, Level = LogLevel.Warning,
        Message = "ConnectionProvider: Failed to delete connection")]
    public static partial IGenericMessage ConnectionDeleteException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Test Connection (8992-8993)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when testing a connection fails.</summary>
    [MessageLogging(EventId = 71017, Level = LogLevel.Warning,
        Message = "ConnectionProvider: Connection test failed for '{connectionName}'")]
    public static partial IGenericMessage ConnectionTestFailed(
        ILogger logger,
        string connectionName);

    /// <summary>Logs when testing a connection fails with exception.</summary>
    [MessageLogging(EventId = 71018, Level = LogLevel.Warning,
        Message = "ConnectionProvider: Connection test failed for '{connectionName}'")]
    public static partial IGenericMessage ConnectionTestException(
        ILogger logger,
        Exception exception,
        string connectionName);
}
