using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Thresholds;

/// <summary>
/// MessageLogging for the threshold evaluator (FDW-3936). EventIds: 11030-11034 (judged, matched, not measured, none attached, loaded), 21009-21010 (a rule that names something that is not there),
/// 71008-71009 (a consequence or the thresholds that could not be used).
/// </summary>
[MessageLoggingTypeCode("THRESHOLD")]
public static partial class ThresholdLog
{
    /// <summary>Logs a subject judged.</summary>
    [MessageLogging(EventId = 11030, Level = LogLevel.Information, Message = "{subjectKind} '{subjectName}' reported its values: {judged} threshold(s) judged, {fired} rule(s) fired, {notMeasured} not measured")]
    public static partial IGenericMessage Judged(ILogger logger, string subjectKind, string subjectName, int judged, int fired, int notMeasured);

    /// <summary>Logs a rule that matched.</summary>
    [MessageLogging(EventId = 11031, Level = LogLevel.Warning, Message = "Rule {ruleNumber} of threshold '{threshold}' matched and fires {escalation}: {reason}")]
    public static partial IGenericMessage RuleMatched(ILogger logger, string threshold, int ruleNumber, string escalation, string reason);

    /// <summary>Logs a rule that was not judged.</summary>
    [MessageLogging(EventId = 11032, Level = LogLevel.Information, Message = "Rule {ruleNumber} of threshold '{threshold}' was not judged: {subjectKind} '{subjectName}' reported no value for {fields}")]
    public static partial IGenericMessage RuleNotMeasured(ILogger logger, string threshold, int ruleNumber, string subjectKind, string subjectName, string fields);

    /// <summary>Logs the thresholds read for a subject.</summary>
    [MessageLogging(EventId = 11034, Level = LogLevel.Trace, Message = "{subjectKind} '{subjectName}' has {count} threshold(s) attached")]
    public static partial IGenericMessage ThresholdsLoaded(ILogger logger, string subjectKind, string subjectName, int count);

    /// <summary>Logs a subject with no threshold.</summary>
    [MessageLogging(EventId = 11033, Level = LogLevel.Trace, Message = "{subjectKind} '{subjectName}' has no threshold attached, so it is not judged")]
    public static partial IGenericMessage NoThreshold(ILogger logger, string subjectKind, string subjectName);

    /// <summary>Logs a rule that fires a consequence that does not exist.</summary>
    [MessageLogging(EventId = 21009, Level = LogLevel.Error, Message = "Rule {ruleNumber} of threshold '{threshold}' names the consequence '{escalation}', which is not one of the escalation types")]
    public static partial IGenericMessage EscalationUnknown(ILogger logger, string threshold, int ruleNumber, string escalation);

    /// <summary>Logs a threshold that cannot be read.</summary>
    [MessageLogging(EventId = 71008, Level = LogLevel.Error, Message = "The thresholds of {subjectKind} '{subjectName}' could not be read: {reason}")]
    public static partial IGenericMessage ThresholdsNotRead(ILogger logger, string subjectKind, string subjectName, string? reason);

    /// <summary>Logs a consequence that failed.</summary>
    [MessageLogging(EventId = 71009, Level = LogLevel.Error, Message = "The {escalation} consequence of rule {ruleNumber} of threshold '{threshold}' failed: {reason}")]
    public static partial IGenericMessage ConsequenceFailed(ILogger logger, string escalation, int ruleNumber, string threshold, string? reason);

    /// <summary>Logs a threshold attached to a subject.</summary>
    [MessageLogging(EventId = 11060, Level = LogLevel.Information, Message = "Threshold '{threshold}' was attached to {subjectKind} {subjectId}")]
    public static partial IGenericMessage ThresholdAttached(ILogger logger, string threshold, string subjectKind, Guid subjectId);

    /// <summary>Logs a threshold detached from a subject.</summary>
    [MessageLogging(EventId = 11061, Level = LogLevel.Information, Message = "Threshold '{threshold}' was detached from {subjectKind} {subjectId}")]
    public static partial IGenericMessage ThresholdDetached(ILogger logger, string threshold, string subjectKind, Guid subjectId);
}
