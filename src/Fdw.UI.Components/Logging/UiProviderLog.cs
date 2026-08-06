using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.UI.Components.Logging;

/// <summary>
/// MessageLogging methods for UI provider operations.
/// Generic operation-level messages shared across all UI providers.
/// EventId range: 8800-8899
/// </summary>
[MessageLoggingTypeCode("UICOMPONENTS3")]
public static partial class UiProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Load Operations (8800-8809)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading a list of entities.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "{providerName}: Loading {entityType} list")]
    public static partial IGenericMessage LoadStarted(
        ILogger logger,
        string providerName,
        string entityType);

    /// <summary>Logs when loading a list of entities fails.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error,
        Message = "{providerName}: Failed to load {entityType} list")]
    public static partial IGenericMessage LoadFailed(
        ILogger logger,
        string providerName,
        string entityType);

    /// <summary>Logs when loading a list of entities fails with exception.</summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Error,
        Message = "{providerName}: Failed to load {entityType} list")]
    public static partial IGenericMessage LoadException(
        ILogger logger,
        Exception exception,
        string providerName,
        string entityType);

    // ═══════════════════════════════════════════════════════════════════════════
    // Detail Operations (8810-8819)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading entity details.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "{providerName}: Loading {entityType} detail for '{entityName}'")]
    public static partial IGenericMessage DetailLoadStarted(
        ILogger logger,
        string providerName,
        string entityType,
        string entityName);

    /// <summary>Logs when loading entity details fails.</summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Error,
        Message = "{providerName}: Failed to load {entityType} detail for '{entityName}'")]
    public static partial IGenericMessage DetailLoadFailed(
        ILogger logger,
        string providerName,
        string entityType,
        string entityName);

    // ═══════════════════════════════════════════════════════════════════════════
    // CRUD Operations (8820-8839)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when creating an entity.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information,
        Message = "{providerName}: Created {entityType} '{entityName}'")]
    public static partial IGenericMessage EntityCreated(
        ILogger logger,
        string providerName,
        string entityType,
        string entityName);

    /// <summary>Logs when creating an entity fails.</summary>
    [MessageLogging(EventId = 71005, Level = LogLevel.Error,
        Message = "{providerName}: Failed to create {entityType}")]
    public static partial IGenericMessage CreateFailed(
        ILogger logger,
        string providerName,
        string entityType);

    /// <summary>Logs when creating an entity fails with exception.</summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Error,
        Message = "{providerName}: Failed to create {entityType}")]
    public static partial IGenericMessage CreateException(
        ILogger logger,
        Exception exception,
        string providerName,
        string entityType);

    /// <summary>Logs when updating an entity.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information,
        Message = "{providerName}: Updated {entityType} '{entityName}'")]
    public static partial IGenericMessage EntityUpdated(
        ILogger logger,
        string providerName,
        string entityType,
        string entityName);

    /// <summary>Logs when updating an entity fails.</summary>
    [MessageLogging(EventId = 71007, Level = LogLevel.Error,
        Message = "{providerName}: Failed to update {entityType} '{entityName}'")]
    public static partial IGenericMessage UpdateFailed(
        ILogger logger,
        string providerName,
        string entityType,
        string entityName);

    /// <summary>Logs when updating an entity fails with exception.</summary>
    [MessageLogging(EventId = 71008, Level = LogLevel.Error,
        Message = "{providerName}: Failed to update {entityType}")]
    public static partial IGenericMessage UpdateException(
        ILogger logger,
        Exception exception,
        string providerName,
        string entityType);

    /// <summary>Logs when deleting an entity.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information,
        Message = "{providerName}: Deleted {entityType} '{entityName}'")]
    public static partial IGenericMessage EntityDeleted(
        ILogger logger,
        string providerName,
        string entityType,
        string entityName);

    /// <summary>Logs when deleting an entity fails.</summary>
    [MessageLogging(EventId = 71009, Level = LogLevel.Error,
        Message = "{providerName}: Failed to delete {entityType} '{entityName}'")]
    public static partial IGenericMessage DeleteFailed(
        ILogger logger,
        string providerName,
        string entityType,
        string entityName);

    /// <summary>Logs when deleting an entity fails with exception.</summary>
    [MessageLogging(EventId = 71010, Level = LogLevel.Error,
        Message = "{providerName}: Failed to delete {entityType}")]
    public static partial IGenericMessage DeleteException(
        ILogger logger,
        Exception exception,
        string providerName,
        string entityType);

    // ═══════════════════════════════════════════════════════════════════════════
    // Generic Operations (8840-8849)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs a generic operation failure.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error,
        Message = "{providerName}: {operationName} failed")]
    public static partial IGenericMessage OperationFailed(
        ILogger logger,
        string providerName,
        string operationName);

    /// <summary>Logs a generic operation failure with exception.</summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error,
        Message = "{providerName}: {operationName} failed")]
    public static partial IGenericMessage OperationException(
        ILogger logger,
        Exception exception,
        string providerName,
        string operationName);

    /// <summary>Logs when testing a connection.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information,
        Message = "{providerName}: Testing connection '{connectionName}'")]
    public static partial IGenericMessage TestingConnection(
        ILogger logger,
        string providerName,
        string connectionName);

    /// <summary>Logs when testing a connection fails.</summary>
    [MessageLogging(EventId = 71011, Level = LogLevel.Error,
        Message = "{providerName}: Connection test failed for '{connectionName}'")]
    public static partial IGenericMessage ConnectionTestFailed(
        ILogger logger,
        string providerName,
        string connectionName);

    /// <summary>Logs when testing a connection fails with exception.</summary>
    [MessageLogging(EventId = 71012, Level = LogLevel.Error,
        Message = "{providerName}: Connection test failed for '{connectionName}'")]
    public static partial IGenericMessage ConnectionTestException(
        ILogger logger,
        Exception exception,
        string providerName,
        string connectionName);

    /// <summary>Logs when toggling an entity state.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information,
        Message = "{providerName}: Toggled {entityType} '{entityName}'")]
    public static partial IGenericMessage EntityToggled(
        ILogger logger,
        string providerName,
        string entityType,
        string entityName);

    /// <summary>Logs when toggling fails with exception.</summary>
    [MessageLogging(EventId = 71013, Level = LogLevel.Error,
        Message = "{providerName}: Failed to toggle {entityType}")]
    public static partial IGenericMessage ToggleException(
        ILogger logger,
        Exception exception,
        string providerName,
        string entityType);

    /// <summary>Logs when saving permissions fails with exception.</summary>
    [MessageLogging(EventId = 71014, Level = LogLevel.Error,
        Message = "{providerName}: Failed to save permissions")]
    public static partial IGenericMessage SavePermissionsException(
        ILogger logger,
        Exception exception,
        string providerName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Configuration Scope (7550-7554)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when a provider's configuration scope changes.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Information,
        Message = "{providerName}: Configuration scope changed from '{previousScope}' to '{newScope}'")]
    public static partial IGenericMessage ScopeChanged(
        ILogger logger,
        string providerName,
        string previousScope,
        string newScope);

    /// <summary>Logs when a provider applies scope filtering to its data.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace,
        Message = "{providerName}: Applied '{scopeName}' scope filter — {totalCount} total, {filteredCount} after scope")]
    public static partial IGenericMessage ScopeFilterApplied(
        ILogger logger,
        string providerName,
        string scopeName,
        int totalCount,
        int filteredCount);
}
