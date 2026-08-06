using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Audit.Logging;

/// <summary>
/// MessageLogging definitions for audit operations.
/// EventId range: 7200-7249
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("AUDIT")]
public static partial class AuditLog
{
    /// <summary>Logs a configuration create audit event.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information, Message = "Configuration created: {entityType} '{entityId}' by {userId}")]
    public static partial IGenericMessage ConfigurationCreated(ILogger logger, string entityType, string entityId, string userId);

    /// <summary>Logs successful creation of an audit record.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug, Message = "Audit record created for {entityType} '{entityId}' create operation")]
    public static partial IGenericMessage AuditRecordCreated(ILogger logger, string entityType, string entityId);

    /// <summary>Logs failure to create an audit record.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Failed to create audit record for {entityType} '{entityId}'")]
    public static partial IGenericMessage AuditRecordCreateFailed(ILogger logger, string entityType, string entityId);

    /// <summary>Logs a configuration update audit event.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information, Message = "Configuration updated: {entityType} '{entityId}' by {userId}")]
    public static partial IGenericMessage ConfigurationUpdated(ILogger logger, string entityType, string entityId, string userId);

    /// <summary>Logs the fields that changed during an update.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug, Message = "Fields changed for {entityType} '{entityId}': {changedFields}")]
    public static partial IGenericMessage FieldsChanged(ILogger logger, string entityType, string entityId, string changedFields);

    /// <summary>Logs a configuration delete audit event.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information, Message = "Configuration deleted: {entityType} '{entityId}' by {userId}")]
    public static partial IGenericMessage ConfigurationDeleted(ILogger logger, string entityType, string entityId, string userId);

    /// <summary>Logs an audit trail query.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Debug, Message = "Audit trail queried for {entityType} '{entityId}'")]
    public static partial IGenericMessage AuditTrailQueried(ILogger logger, string entityType, string entityId);

    /// <summary>Logs the number of audit records retrieved.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information, Message = "Retrieved {count} audit records for {entityType}")]
    public static partial IGenericMessage AuditRecordsRetrieved(ILogger logger, int count, string? entityType);

    /// <summary>Logs an audit records list query.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Trace, Message = "Listing audit records: entityType={entityType}, action={action}, limit={limit}")]
    public static partial IGenericMessage AuditRecordsListed(ILogger logger, string? entityType, string? action, int limit);

    /// <summary>Logs failure to list audit records.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error, Message = "Failed to list audit records")]
    public static partial IGenericMessage AuditRecordsListFailed(ILogger logger);
}
