using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Escalation;

/// <summary>
/// MessageLogging for the alerts escalation policies walk (FDW-3863). EventIds: 11020-11025 (alerts opened, levels sent, alerts taken or closed), 21007-21008 and 31005-31006
/// (a policy or alert that cannot be used or found), 41005 (an alert whose state does not allow the action), 61001-61003 (settings and the store), 71005-71007 (a store
/// or a send that failed).
/// </summary>
[MessageLoggingTypeCode("ESCALATIONALERT")]
public static partial class EscalationAlertLog
{
    /// <summary>Logs an alert opened.</summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Warning, Message = "Alert {alertId} opened for threshold '{threshold}' of {subjectKind} '{subjectName}' on policy '{policy}'")]
    public static partial IGenericMessage AlertOpened(ILogger logger, Guid alertId, string threshold, string subjectKind, string subjectName, string policy);

    /// <summary>Logs a threshold that fired while its alert was open.</summary>
    [MessageLogging(EventId = 11021, Level = LogLevel.Information, Message = "Threshold '{threshold}' of {subjectKind} '{subjectName}' fired again; alert {alertId} is already open")]
    public static partial IGenericMessage AlertAlreadyOpen(ILogger logger, string threshold, string subjectKind, string subjectName, Guid alertId);

    /// <summary>Logs a threshold that fired within its policy's cooldown.</summary>
    [MessageLogging(EventId = 11022, Level = LogLevel.Information, Message = "Threshold '{threshold}' of {subjectKind} '{subjectName}' fired, but policy '{policy}' is cooling down since the last alert was resolved")]
    public static partial IGenericMessage AlertCoolingDown(ILogger logger, string threshold, string subjectKind, string subjectName, string policy);

    /// <summary>Logs a level sent.</summary>
    [MessageLogging(EventId = 11023, Level = LogLevel.Warning, Message = "Alert {alertId} reached level {level} of policy '{policy}' and sent it through '{channel}'")]
    public static partial IGenericMessage LevelSent(ILogger logger, Guid alertId, int level, string policy, string channel);

    /// <summary>Logs an alert acknowledged.</summary>
    [MessageLogging(EventId = 11024, Level = LogLevel.Information, Message = "Alert {alertId} was acknowledged by {actor}")]
    public static partial IGenericMessage AlertAcknowledged(ILogger logger, Guid alertId, string actor);

    /// <summary>Logs an alert resolved.</summary>
    [MessageLogging(EventId = 11025, Level = LogLevel.Information, Message = "Alert {alertId} was resolved by {actor}")]
    public static partial IGenericMessage AlertResolved(ILogger logger, Guid alertId, string actor);

    /// <summary>Logs a policy that is not enabled, so no alert can use it (FDW-4874).</summary>
    [MessageLogging(EventId = 21007, Level = LogLevel.Warning, Message = "Escalation policy '{policy}' is not enabled, so no alert can use it",
        ClientMessage = "Escalation policy '{policy}' is not enabled; enable it before an alert can use it")]
    public static partial IGenericMessage PolicyNotEnabled(ILogger logger, string policy);

    /// <summary>Logs a policy that states no level within its maximum level, so it has nothing to send (FDW-4874).</summary>
    [MessageLogging(EventId = 21009, Level = LogLevel.Warning, Message = "Escalation policy '{policy}' states no level within its maximum level, so it has nothing to send",
        ClientMessage = "Escalation policy '{policy}' states no level within its maximum level; add a level before an alert can use it")]
    public static partial IGenericMessage PolicyHasNoLevel(ILogger logger, string policy);

    /// <summary>Logs a state that is not one of the alert states.</summary>
    [MessageLogging(EventId = 21008, Level = LogLevel.Warning, Message = "'{state}' is not an escalation alert state; the states are Firing, Acknowledged and Resolved")]
    public static partial IGenericMessage StateUnknown(ILogger logger, string state);

    /// <summary>Logs a policy that does not exist.</summary>
    [MessageLogging(EventId = 31005, Level = LogLevel.Warning, Message = "Escalation policy '{policy}' was not found")]
    public static partial IGenericMessage PolicyNotFound(ILogger logger, string policy);

    /// <summary>Logs an alert that does not exist.</summary>
    [MessageLogging(EventId = 31006, Level = LogLevel.Warning, Message = "Escalation alert {alertId} was not found")]
    public static partial IGenericMessage AlertNotFound(ILogger logger, Guid alertId);

    /// <summary>Logs an action an alert's state does not allow.</summary>
    [MessageLogging(EventId = 41005, Level = LogLevel.Warning, Message = "Escalation alert {alertId} is {state}, which does not allow {action}")]
    public static partial IGenericMessage AlertStateRefused(ILogger logger, Guid alertId, string state, string action);

    /// <summary>Logs a host that names no operational store.</summary>
    [MessageLogging(EventId = 61001, Level = LogLevel.Error, Message = "The host names no operational store, so escalation alerts cannot be kept or read")]
    public static partial IGenericMessage StoreNotNamed(ILogger logger);

    /// <summary>Logs the notification priority setting that is not set or names no priority.</summary>
    [MessageLogging(EventId = 61002, Level = LogLevel.Error, Message = "Alert {alertId} level {level} was not sent: the setting {setting} is not set to a notification priority (found '{value}')")]
    public static partial IGenericMessage PrioritySettingMissing(ILogger logger, Guid alertId, int level, string setting, string? value);

    /// <summary>Logs the advance interval setting that is not set or not positive.</summary>
    [MessageLogging(EventId = 61003, Level = LogLevel.Error, Message = "The escalation timer does not start: the setting {setting} is not set to a positive number of seconds")]
    public static partial IGenericMessage AdvanceIntervalMissing(ILogger logger, string setting);

    /// <summary>Logs an alert that could not be kept.</summary>
    [MessageLogging(EventId = 71005, Level = LogLevel.Error, Message = "Escalation alert {alertId} could not be written: {reason}")]
    public static partial IGenericMessage AlertNotKept(ILogger logger, Guid alertId, string? reason);

    /// <summary>Logs alerts that could not be read.</summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Error, Message = "Escalation alerts could not be read: {reason}")]
    public static partial IGenericMessage AlertsNotRead(ILogger logger, string? reason);

    /// <summary>Logs a level that could not be sent.</summary>
    [MessageLogging(EventId = 71007, Level = LogLevel.Error, Message = "Alert {alertId} level {level} of policy '{policy}' could not be sent through '{channel}': {reason}")]
    public static partial IGenericMessage LevelNotSent(ILogger logger, Guid alertId, int level, string policy, string channel, string? reason);
}
