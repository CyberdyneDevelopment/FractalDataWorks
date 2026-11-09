using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Logging;

/// <summary>
/// MessageLogging for the disabled state of a connection (FDW-3951). EventId range: 12230-12249.
/// </summary>
[MessageLoggingTypeCode("CONNECTIONS")]
public static partial class ConnectionDisabledLog
{
    /// <summary>Logs that the state of a connection could not be read, so it cannot be told whether it is disabled.</summary>
    [MessageLogging(EventId = 90008, Level = LogLevel.Error, Message = "The disabled state of connection {connectionId} could not be read: {reason}", ClientMessage = "The disabled state of connection {connectionId} could not be read")]
    public static partial IGenericMessage ReadFailed(ILogger logger, Guid connectionId, string reason);

    /// <summary>Logs that the states of many connections could not be read, so it cannot be told which are disabled (FDW-4290).</summary>
    [MessageLogging(EventId = 90005, Level = LogLevel.Error, Message = "The disabled states of {count} connection(s) could not be read: {reason}", ClientMessage = "The disabled states of {count} connection(s) could not be read")]
    public static partial IGenericMessage ReadManyFailed(ILogger logger, int count, string reason);

    /// <summary>Logs that a connection was asked for while disabled and was refused.</summary>
    [MessageLogging(EventId = 12231, Level = LogLevel.Warning, Message = "Connection '{connectionName}' ({connectionId}) is disabled since {disabledAt}: {reason}")]
    public static partial IGenericMessage Refused(ILogger logger, string connectionName, Guid connectionId, string disabledAt, string reason);

    /// <summary>Logs that a connection was disabled.</summary>
    [MessageLogging(EventId = 12232, Level = LogLevel.Warning, Message = "Connection {connectionId} disabled by {disabledBy}: {reason}")]
    public static partial IGenericMessage Disabled(ILogger logger, Guid connectionId, string disabledBy, string reason);

    /// <summary>Logs that a connection to disable was already disabled and was left as it is.</summary>
    [MessageLogging(EventId = 12233, Level = LogLevel.Information, Message = "Connection {connectionId} is already disabled; the first reason stands")]
    public static partial IGenericMessage AlreadyDisabled(ILogger logger, Guid connectionId);

    /// <summary>Logs that the disabled state of a connection could not be saved.</summary>
    [MessageLogging(EventId = 90006, Level = LogLevel.Error, Message = "The disabled state of connection {connectionId} was not saved: {reason}", ClientMessage = "The disabled state of connection {connectionId} was not saved")]
    public static partial IGenericMessage DisableNotSaved(ILogger logger, Guid connectionId, string reason);

    /// <summary>Logs that a connection was enabled again.</summary>
    [MessageLogging(EventId = 12235, Level = LogLevel.Information, Message = "Connection {connectionId} enabled again by {enabledBy}: {reason}")]
    public static partial IGenericMessage Enabled(ILogger logger, Guid connectionId, string enabledBy, string reason);

    /// <summary>Logs that the disabled state of a connection names no tenant, so it cannot be written as the tenant it belongs to and the connection was not enabled (FDW-4385).</summary>
    [MessageLogging(EventId = 60002, Level = LogLevel.Error, Message = "The disabled state of connection {connectionId} names no tenant, so it was not enabled", ClientMessage = "The disabled state of connection {connectionId} names no tenant, so it was not enabled")]
    public static partial IGenericMessage StateTenantMissing(ILogger logger, Guid connectionId);

    /// <summary>Logs that the store's row-level security refused the write of a connection's state because the connection belongs to another tenant. The number is in the Forbidden category (10) (FDW-4459).</summary>
    [MessageLogging(EventId = 103210, Level = LogLevel.Warning, Message = "Connection {connectionId} belongs to another tenant, so its state cannot be changed from this one",
        ClientMessage = "The connection belongs to another tenant, so its state cannot be changed from this one")]
    public static partial IGenericMessage EnableRefusedBySecurityPolicy(ILogger logger, Guid connectionId);

    /// <summary>Logs that a connection to enable was not disabled and was left as it is.</summary>
    [MessageLogging(EventId = 12236, Level = LogLevel.Information, Message = "Connection {connectionId} is not disabled; nothing to enable")]
    public static partial IGenericMessage NotDisabled(ILogger logger, Guid connectionId);

    /// <summary>Logs that enabling a connection could not be saved.</summary>
    [MessageLogging(EventId = 90007, Level = LogLevel.Error, Message = "Enabling connection {connectionId} was not saved: {reason}", ClientMessage = "Enabling connection {connectionId} was not saved")]
    public static partial IGenericMessage EnableNotSaved(ILogger logger, Guid connectionId, string reason);

    /// <summary>Logs that a disable or enable came without the text that says why, or who.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Error, Message = "Connection {connectionId}: the {missing} is required to {operation} a connection", ClientMessage = "Connection {connectionId}: the {missing} is required to {operation} a connection")]
    public static partial IGenericMessage TextMissing(ILogger logger, Guid connectionId, string missing, string operation);

    /// <summary>Logs that the configuration gateway the state is kept in could not be reached.</summary>
    [MessageLogging(EventId = 60003, Level = LogLevel.Error, Message = "The configuration gateway '{gatewayName}' for the disabled state of connections is not available: {reason}", ClientMessage = "The configuration gateway '{gatewayName}' for the disabled state of connections is not available")]
    public static partial IGenericMessage GatewayUnavailable(ILogger logger, string gatewayName, string reason);

    /// <summary>Logs that the health monitor left a disabled connection unprobed.</summary>
    [MessageLogging(EventId = 12241, Level = LogLevel.Debug, Message = "Connection '{connectionName}' is disabled; the health monitor does not probe it")]
    public static partial IGenericMessage ProbeSkipped(ILogger logger, string connectionName);

    /// <summary>Logs that a threshold consequence to disable a connection named a subject that is not a connection.</summary>
    [MessageLogging(EventId = 90002, Level = LogLevel.Error, Message = "The threshold '{thresholdName}' fired to disable a connection on a '{subjectKind}' subject; only a Connection subject, or a Pipeline subject whose values name the failed connection, can disable one", ClientMessage = "The threshold '{thresholdName}' fired to disable a connection on a '{subjectKind}' subject; only a Connection subject, or a Pipeline subject whose values name the failed connection, can disable one")]
    public static partial IGenericMessage SubjectNotAConnection(ILogger logger, string thresholdName, string subjectKind);

    /// <summary>Logs that the tenant a connection to disable belongs to could not be read from its row.</summary>
    [MessageLogging(EventId = 90009, Level = LogLevel.Error, Message = "The tenant of connection {connectionId} could not be read, so it was not disabled: {reason}", ClientMessage = "The tenant of connection {connectionId} could not be read, so it was not disabled")]
    public static partial IGenericMessage TenantNotRead(ILogger logger, Guid connectionId, string reason);

    /// <summary>Logs that a connection to disable has no row the reader can see.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Error, Message = "Connection {connectionId} has no current row the reader can see, so it was not disabled", ClientMessage = "Connection {connectionId} has no current row the reader can see, so it was not disabled")]
    public static partial IGenericMessage ConnectionNotFound(ILogger logger, Guid connectionId);

    /// <summary>Logs that a disable was refused because the connection belongs to another tenant than the one asking (FDW-4192). The number is in the Forbidden category (10).</summary>
    [MessageLogging(EventId = 100230, Level = LogLevel.Warning, Message = "Connection {connectionId} belongs to tenant {ownerTenantId}, not to tenant {requestingTenantId}, so it was not disabled")]
    public static partial IGenericMessage OtherTenantsConnection(ILogger logger, Guid connectionId, Guid ownerTenantId, Guid requestingTenantId);

    /// <summary>Logs that the tenant of a threshold that fired to disable a connection could not be read, so it could not be told whether it is the global tenant.</summary>
    [MessageLogging(EventId = 90001, Level = LogLevel.Error, Message = "The tenant {requestingTenantId} of threshold '{thresholdName}' could not be read, so it was not told whether it is the global tenant and the connection was not disabled: {reason}", ClientMessage = "The tenant of threshold '{thresholdName}' could not be read, so it was not told whether it is the global tenant and the connection was not disabled")]
    public static partial IGenericMessage RequestingTenantNotRead(ILogger logger, Guid requestingTenantId, string thresholdName, string reason);

    /// <summary>Logs that a default of disabling a connection by a threshold is declared nowhere, neither by the option nor by the host.</summary>
    [MessageLogging(EventId = 60001, Level = LogLevel.Error, Message = "The default '{defaultName}' of disabling a connection by a threshold is declared by neither the option nor the host, so the connection was not disabled", ClientMessage = "The default '{defaultName}' of disabling a connection by a threshold is declared by neither the option nor the host, so the connection was not disabled")]
    public static partial IGenericMessage DefaultNotDeclared(ILogger logger, string defaultName);

    /// <summary>Logs that a threshold consequence to disable a connection came from a run that recorded no failed connection.</summary>
    [MessageLogging(EventId = 90003, Level = LogLevel.Error, Message = "The threshold '{thresholdName}' fired to disable a connection on {subjectKind} '{subjectName}', but its values name no failed connection ({valueName})", ClientMessage = "The threshold '{thresholdName}' fired to disable a connection on {subjectKind} '{subjectName}', but its values name no failed connection ({valueName})")]
    public static partial IGenericMessage FailedConnectionNotNamed(ILogger logger, string thresholdName, string subjectKind, string subjectName, string valueName);

    /// <summary>Logs that the failed connection a threshold's values name is not a connection Id.</summary>
    [MessageLogging(EventId = 90004, Level = LogLevel.Error, Message = "The threshold '{thresholdName}' fired to disable a connection, but the value {valueName} on {subjectKind} '{subjectName}' is not a connection Id", ClientMessage = "The threshold '{thresholdName}' fired to disable a connection, but the value {valueName} on {subjectKind} '{subjectName}' is not a connection Id")]
    public static partial IGenericMessage FailedConnectionNotAnId(ILogger logger, string thresholdName, string subjectKind, string subjectName, string valueName);
}
