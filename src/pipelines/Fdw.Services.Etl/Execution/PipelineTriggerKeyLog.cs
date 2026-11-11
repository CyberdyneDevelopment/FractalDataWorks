using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// MessageLogging for the key of a scheduled fire (FDW-4317): the refusals a trigger answers by it and the failures of keeping it.
/// EventId range: 22180-22183, 42180, 60330-60331, 71340-71341.
/// </summary>
[MessageLoggingTypeCode("PIPELINERUN")]
public static partial class PipelineTriggerKeyLog
{
    /// <summary>Logs a scheduled fire that carries no key, which is refused.</summary>
    [MessageLogging(EventId = 22180, Level = LogLevel.Warning,
        Message = "The {source} trigger of pipeline '{pipeline}' carries no trigger key, and this source requires one, so it is refused",
        ClientMessage = "A {source} trigger must carry a trigger key (the schedule's Id and the time of the fire)")]
    public static partial IGenericMessage KeyRequired(ILogger logger, string source, string pipeline);

    /// <summary>Logs a trigger from a source that takes no key that carries one, which is refused.</summary>
    [MessageLogging(EventId = 22181, Level = LogLevel.Warning,
        Message = "The {source} trigger of pipeline '{pipeline}' carries a trigger key, and this source takes none, so it is refused",
        ClientMessage = "A {source} trigger must not carry a trigger key")]
    public static partial IGenericMessage KeyNotAllowed(ILogger logger, string source, string pipeline);

    /// <summary>Logs a fire that is older than the time its key is remembered, which is refused.</summary>
    [MessageLogging(EventId = 22182, Level = LogLevel.Warning,
        Message = "The trigger key '{key}' is for a fire due {fireTime}, older than the {retentionSeconds} seconds the setting '{setting}' keeps keys for, so it is refused as expired",
        ClientMessage = "The trigger key is for a fire older than the host remembers; it is refused as expired")]
    public static partial IGenericMessage KeyExpired(ILogger logger, string key, DateTimeOffset fireTime, int retentionSeconds, string setting);

    /// <summary>Logs a scheduled fire that names no fire time, which is refused.</summary>
    [MessageLogging(EventId = 22183, Level = LogLevel.Warning,
        Message = "The trigger key '{key}' of pipeline '{pipeline}' names no fire time, so it cannot be checked against the time keys are remembered and is refused",
        ClientMessage = "A trigger key must come with the time of the fire")]
    public static partial IGenericMessage FireTimeMissing(ILogger logger, string key, string pipeline);

    /// <summary>Logs a fire whose key an earlier call holds without a run yet, so the retry is refused until that call finishes.</summary>
    [MessageLogging(EventId = 42180, Level = LogLevel.Warning,
        Message = "The trigger key '{key}' is held by an earlier call that has not made its run yet, so this call is refused; the caller may retry",
        ClientMessage = "An earlier call with this trigger key has not finished; retry shortly")]
    public static partial IGenericMessage KeyInProgress(ILogger logger, string key);

    /// <summary>Logs the retention setting that is absent or not positive, so no key can be checked.</summary>
    [MessageLogging(EventId = 60330, Level = LogLevel.Error,
        Message = "Trigger keys are not checked: setting '{setting}' has no positive value, and no time is assumed")]
    public static partial IGenericMessage RetentionNotSet(ILogger logger, string setting);

    /// <summary>Logs the operational store that is not named, so no key can be read or written.</summary>
    [MessageLogging(EventId = 60331, Level = LogLevel.Error,
        Message = "The operational connection is not named, so trigger keys cannot be read or written")]
    public static partial IGenericMessage StoreNotNamed(ILogger logger);

    /// <summary>Logs a key that could not be written or read.</summary>
    [MessageLogging(EventId = 71340, Level = LogLevel.Error,
        Message = "The trigger key '{key}' could not be kept: {reason}")]
    public static partial IGenericMessage KeyNotKept(ILogger logger, string key, string? reason);

    /// <summary>Logs a key that could not be bound to the run it started or given back.</summary>
    [MessageLogging(EventId = 71341, Level = LogLevel.Error,
        Message = "The trigger key '{key}' could not be bound to run {executionId}, or given back: {reason}")]
    public static partial IGenericMessage KeyNotBound(ILogger logger, string key, Guid executionId, string? reason);

    /// <summary>Error: claiming, binding or releasing a trigger key needs the key; an empty key is refused.</summary>
    [MessageLogging(EventId = 21804, Level = LogLevel.Error,
        Message = "Trigger key {operation} needs the key, and the key given is empty")]
    public static partial IGenericMessage TriggerKeyRequired(ILogger logger, string operation);
}
