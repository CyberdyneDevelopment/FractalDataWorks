using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for the slot role (FDW-5846): the role file that says whether this copy of an A/B slot app is live or standby, and the
/// workers that do nothing unless it says live.
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class SlotRoleLog
{
    /// <summary>The slot file does not state where the role file is.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="slotFilePath">The slot file.</param>
    [MessageLogging(EventId = 61116, Level = LogLevel.Error,
        Message = "The slot file '{slotFilePath}' states no role_file, so this copy cannot tell whether it is live or standby and its background work cannot be gated. Infra writes role_file into each slot's file")]
    public static partial IGenericMessage RoleFileNotStated(ILogger logger, string slotFilePath);

    /// <summary>The role file cannot be read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="roleFile">The role file.</param>
    /// <param name="reason">Why the read failed.</param>
    [MessageLogging(EventId = 61117, Level = LogLevel.Error,
        Message = "The role file '{roleFile}' cannot be read: {reason}. This copy's background work does nothing until it can be")]
    public static partial IGenericMessage RoleFileUnreadable(ILogger logger, string roleFile, string reason);

    /// <summary>The role file says something other than live or standby.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="roleFile">The role file.</param>
    /// <param name="content">What it holds.</param>
    [MessageLogging(EventId = 61118, Level = LogLevel.Error,
        Message = "The role file '{roleFile}' holds '{content}', not 'live' or 'standby'. This copy's background work does nothing until it says one of them")]
    public static partial IGenericMessage RoleFileSaysUnknown(ILogger logger, string roleFile, string content);

    /// <summary>A role-gated worker states an interval of no time.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="worker">The worker.</param>
    /// <param name="interval">The interval it stated.</param>
    [MessageLogging(EventId = 21110, Level = LogLevel.Error,
        Message = "Worker '{worker}' states the interval {interval}, which is not positive, so its passes would run without pause; it does not run")]
    public static partial IGenericMessage IntervalNotPositive(ILogger logger, string worker, TimeSpan interval);

    /// <summary>A pass threw.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">What it threw.</param>
    /// <param name="worker">The worker.</param>
    [MessageLogging(EventId = 91280, Level = LogLevel.Error,
        Message = "A pass of worker '{worker}' failed; the next pass runs at its interval")]
    public static partial IGenericMessage PassFailed(ILogger logger, Exception exception, string worker);

    /// <summary>The role was read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="roleFile">The role file.</param>
    /// <param name="role">What it says.</param>
    [MessageLogging(EventId = 11592, Level = LogLevel.Trace,
        Message = "The role file '{roleFile}' says '{role}'")]
    public static partial IGenericMessage RoleRead(ILogger logger, string roleFile, string role);

    /// <summary>The role gate was registered for a host that is on a slot.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="roleFile">The role file it reads on every call.</param>
    [MessageLogging(EventId = 11593, Level = LogLevel.Information,
        Message = "This host runs on an A/B slot: its background work runs only while the role file '{roleFile}' says live")]
    public static partial IGenericMessage GateOnSlot(ILogger logger, string roleFile);

    /// <summary>The role gate was registered for a host that is not on a slot.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 11594, Level = LogLevel.Information,
        Message = "This host does not run on an A/B slot (APP_SLOT_FILE is not set): its background work is not gated")]
    public static partial IGenericMessage GateNotOnSlot(ILogger logger);

    /// <summary>A pass was skipped because this copy is standby.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="worker">The worker.</param>
    [MessageLogging(EventId = 11595, Level = LogLevel.Trace,
        Message = "Worker '{worker}' skips this pass: this copy is standby")]
    public static partial IGenericMessage PassSkippedStandby(ILogger logger, string worker);

    /// <summary>A pass was skipped because the role could not be read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="worker">The worker.</param>
    [MessageLogging(EventId = 11596, Level = LogLevel.Warning,
        Message = "Worker '{worker}' skips this pass: the role of this copy could not be read")]
    public static partial IGenericMessage PassSkippedRoleUnknown(ILogger logger, string worker);
}
